namespace SqlParser;

internal sealed class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private int _index;
    private SyntaxToken _current;

    private Parser(IReadOnlyList<SyntaxToken> tokens)
    {
        _tokens = tokens;
        _current = tokens[0];
        _index = 1;
    }

    public static Query Parse(IReadOnlyList<SyntaxToken> tokens)
    {
        var parser = new Parser(tokens);
        var query = parser.ParseQuery();
        if (parser._current.Kind == SyntaxKind.Semicolon)
        {
            parser.Advance();
        }

        parser.Expect(SyntaxKind.EndOfFile);

        return query;
    }

    private Query ParseQuery(int minBindingPower = 0)
    {
        if (minBindingPower == 0 && _current.Kind == SyntaxKind.WithKeyword)
        {
            return ParseWithQuery();
        }

        return ParseSetOp(minBindingPower);
    }

    private WithQuery ParseWithQuery()
    {
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var recursive = _current.Kind == SyntaxKind.RecursiveKeyword ? Advance() : (SyntaxToken?)null;
        var ctes = new List<CommonTableExpression> { ParseCte() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            ctes.Add(ParseCte());
        }

        return new WithQuery
        {
            WithKeyword = withKeyword,
            RecursiveKeyword = recursive,
            Ctes = ctes,
            Query = ParseSetOp(),
        };
    }

    private CommonTableExpression ParseCte()
    {
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(Expect(SyntaxKind.Identifier));
            }

            columns = names;
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        return new CommonTableExpression
        {
            Name = name,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            AsKeyword = Expect(SyntaxKind.AsKeyword),
            OpenQuery = Expect(SyntaxKind.OpenParen),
            Query = ParseQuery(),
            CloseQuery = Expect(SyntaxKind.CloseParen),
        };
    }

    private Query ParseSetOp(int minBindingPower = 0)
    {
        Query left = ParseSetPrimary();
        while (true)
        {
            var bindingPower = SetOpBindingPower(_current.Kind);
            if (bindingPower == 0 || bindingPower < minBindingPower)
            {
                break;
            }

            var op = Advance();
            var all = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
            left = new SetOperation
            {
                Left = left,
                Operator = op,
                AllKeyword = all,
                Right = ParseQuery(bindingPower + 1),
            };
        }

        return left;
    }

    private Query ParseSetPrimary()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseParenQuery();
        }

        return ParseSelectStatement();
    }

    private ParenQuery ParseParenQuery()
    {
        return new ParenQuery
        {
            OpenParen = Expect(SyntaxKind.OpenParen),
            Inner = ParseQuery(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private const int UnionBindingPower = 1;
    private const int IntersectBindingPower = 2;

    private static bool IsQueryStart(SyntaxKind kind) =>
        kind is SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword;

    private static int SetOpBindingPower(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UnionKeyword or SyntaxKind.ExceptKeyword => UnionBindingPower,
        SyntaxKind.IntersectKeyword => IntersectBindingPower,
        _ => 0,
    };

    private SelectStatement ParseSelectStatement()
    {
        var selectKeyword = Expect(SyntaxKind.SelectKeyword);
        var distinct = _current.Kind == SyntaxKind.DistinctKeyword ? Advance() : (SyntaxToken?)null;
        var selectList = ParseSelectList();
        SyntaxToken? fromKeyword = null;
        TableSource? from = null;
        IReadOnlyList<JoinClause> joins = [];
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            from = ParseTableSource();
            joins = ParseJoins();
        }
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        var groupBy = _current.Kind == SyntaxKind.GroupKeyword ? ParseGroupBy() : null;
        var having = _current.Kind == SyntaxKind.HavingKeyword ? ParseHaving() : null;
        var orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        LimitClause? limit = null;
        OffsetClause? offset = null;
        while (true)
        {
            if (limit is null && _current.Kind == SyntaxKind.LimitKeyword)
            {
                limit = ParseLimit();
                continue;
            }

            if (offset is null && _current.Kind == SyntaxKind.OffsetKeyword)
            {
                offset = ParseOffset();
                continue;
            }

            break;
        }

        return new SelectStatement
        {
            SelectKeyword = selectKeyword,
            DistinctKeyword = distinct,
            SelectList = selectList,
            FromKeyword = fromKeyword,
            From = from,
            Joins = joins,
            Where = where,
            GroupBy = groupBy,
            Having = having,
            OrderBy = orderBy,
            Limit = limit,
            Offset = offset,
        };
    }

    private TableSource ParseTableSource()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseDerivedTable();
        }

        return ParseTableReference();
    }

    private DerivedTable ParseDerivedTable()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? asKeyword = null;
        SyntaxToken alias;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }
        else
        {
            throw new SqlParseException("Expected alias after derived table", _current.Position);
        }

        return new DerivedTable
        {
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private TableReference ParseTableReference()
    {
        var nameParts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            nameParts.Add(Expect(SyntaxKind.Identifier));
        }

        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }

        return new TableReference
        {
            NameParts = nameParts,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private IReadOnlyList<JoinClause> ParseJoins()
    {
        if (!IsJoinStart(_current.Kind))
        {
            return [];
        }

        var joins = new List<JoinClause>();
        while (IsJoinStart(_current.Kind))
        {
            joins.Add(ParseJoin());
        }

        return joins;
    }

    private JoinClause ParseJoin()
    {
        var natural = _current.Kind == SyntaxKind.NaturalKeyword ? Advance() : (SyntaxToken?)null;
        SyntaxToken? joinType = null;
        if (_current.Kind is SyntaxKind.InnerKeyword or SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword
            or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword)
        {
            joinType = Advance();
        }

        SyntaxToken? outer = null;
        if (_current.Kind == SyntaxKind.OuterKeyword)
        {
            if (joinType is not { Kind: SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword })
            {
                throw new SqlParseException("OUTER is only valid after LEFT, RIGHT, or FULL", _current.Position);
            }

            outer = Advance();
        }

        var joinKeyword = Expect(SyntaxKind.JoinKeyword);
        var table = ParseTableSource();
        var constraint = ParseJoinConstraint(natural, joinType);

        return new JoinClause
        {
            NaturalKeyword = natural,
            JoinType = joinType,
            OuterKeyword = outer,
            JoinKeyword = joinKeyword,
            Table = table,
            Constraint = constraint,
        };
    }

    private JoinConstraint? ParseJoinConstraint(SyntaxToken? natural, SyntaxToken? joinType)
    {
        var isCross = joinType is { Kind: SyntaxKind.CrossKeyword };
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            if (natural is not null || isCross)
            {
                throw new SqlParseException("ON is not valid with NATURAL or CROSS JOIN", _current.Position);
            }

            return new OnConstraint
            {
                OnKeyword = Advance(),
                Condition = ParseExpression(),
            };
        }

        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            if (natural is not null || isCross)
            {
                throw new SqlParseException("USING is not valid with NATURAL or CROSS JOIN", _current.Position);
            }

            return ParseUsingConstraint();
        }

        if (natural is null && !isCross)
        {
            throw new SqlParseException("Expected ON or USING", _current.Position);
        }

        return null;
    }

    private UsingConstraint ParseUsingConstraint()
    {
        var usingKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(Expect(SyntaxKind.Identifier));
        }

        return new UsingConstraint
        {
            UsingKeyword = usingKeyword,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private static bool IsJoinStart(SyntaxKind kind) =>
        kind is SyntaxKind.NaturalKeyword or SyntaxKind.InnerKeyword or SyntaxKind.LeftKeyword
            or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword
            or SyntaxKind.JoinKeyword;

    private IReadOnlyList<SelectItem> ParseSelectList()
    {
        var items = new List<SelectItem> { ParseSelectItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseSelectItem());
        }

        return items;
    }

    private SelectItem ParseSelectItem()
    {
        var expression = _current.Kind == SyntaxKind.Star
            ? new StarExpression { Star = Advance() }
            : ParseExpression();
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }

        return new SelectItem
        {
            Expression = expression,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private GroupByClause ParseGroupBy()
    {
        return new GroupByClause
        {
            GroupKeyword = Expect(SyntaxKind.GroupKeyword),
            ByKeyword = Expect(SyntaxKind.ByKeyword),
            Keys = ParseExpressionList(),
        };
    }

    private OrderByClause ParseOrderBy()
    {
        var orderKeyword = Expect(SyntaxKind.OrderKeyword);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var items = new List<OrderByItem> { ParseOrderByItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseOrderByItem());
        }

        return new OrderByClause
        {
            OrderKeyword = orderKeyword,
            ByKeyword = byKeyword,
            Items = items,
        };
    }

    private OrderByItem ParseOrderByItem()
    {
        var expression = ParseExpression();
        SyntaxToken? direction = null;
        if (_current.Kind is SyntaxKind.AscKeyword or SyntaxKind.DescKeyword)
        {
            direction = Advance();
        }

        return new OrderByItem
        {
            Expression = expression,
            Direction = direction,
        };
    }

    private LimitClause ParseLimit()
    {
        return new LimitClause
        {
            LimitKeyword = Expect(SyntaxKind.LimitKeyword),
            Count = ParseExpression(),
        };
    }

    private OffsetClause ParseOffset()
    {
        return new OffsetClause
        {
            OffsetKeyword = Expect(SyntaxKind.OffsetKeyword),
            Count = ParseExpression(),
        };
    }

    private HavingClause ParseHaving()
    {
        return new HavingClause
        {
            HavingKeyword = Expect(SyntaxKind.HavingKeyword),
            Expression = ParseExpression(),
        };
    }

    private IReadOnlyList<Expression> ParseExpressionList()
    {
        var items = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseExpression());
        }

        return items;
    }

    private WhereClause ParseWhereClause()
    {
        return new WhereClause
        {
            WhereKeyword = Expect(SyntaxKind.WhereKeyword),
            Expression = ParseExpression(),
        };
    }

    private Expression ParseExpression(int minBindingPower = 0)
    {
        var left = ParsePrefix();
        while (true)
        {
            if (_current.Kind == SyntaxKind.DoubleColonToken && ColonCastBindingPower >= minBindingPower)
            {
                var doubleColon = Advance();
                left = new ColonCastExpression
                {
                    Expression = left,
                    DoubleColon = doubleColon,
                    Type = ParseDataType(),
                };
                continue;
            }

            if (_current.Kind == SyntaxKind.Dot && DotBindingPower >= minBindingPower)
            {
                var dot = Advance();
                if (_current.Kind == SyntaxKind.Star)
                {
                    left = new QualifiedStarExpression
                    {
                        Target = left,
                        Dot = dot,
                        Star = Advance(),
                    };
                    continue;
                }

                left = new MemberAccessExpression
                {
                    Target = left,
                    Dot = dot,
                    Member = Expect(SyntaxKind.Identifier),
                };
                continue;
            }

            if (ComparisonBindingPower >= minBindingPower)
            {
                switch (_current.Kind)
                {
                    case SyntaxKind.BetweenKeyword:
                        left = ParseBetween(left);
                        continue;
                    case SyntaxKind.InKeyword:
                        left = ParseIn(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.InKeyword:
                        left = ParseIn(left);
                        continue;
                    case SyntaxKind.LikeKeyword:
                        left = ParseLike(left);
                        continue;
                    case SyntaxKind.NotKeyword when NextKind == SyntaxKind.LikeKeyword:
                        left = ParseLike(left);
                        continue;
                    case SyntaxKind.IsKeyword:
                        left = ParseIsNull(left);
                        continue;
                }
            }

            var bindingPower = BindingPower(_current.Kind);
            if (bindingPower == 0 || bindingPower < minBindingPower)
            {
                break;
            }

            var operatorToken = Advance();
            if (bindingPower == ComparisonBindingPower
                && _current.Kind is SyntaxKind.AllKeyword or SyntaxKind.AnyKeyword or SyntaxKind.SomeKeyword)
            {
                left = ParseQuantifiedSubquery(left, operatorToken);
                continue;
            }

            var right = ParseExpression(bindingPower + 1);
            left = new BinaryExpression
            {
                Left = left,
                OperatorToken = operatorToken,
                Right = right,
            };
        }

        return left;
    }

    private LikeExpression ParseLike(Expression target)
    {
        return new LikeExpression
        {
            Target = target,
            NotKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null,
            LikeKeyword = Expect(SyntaxKind.LikeKeyword),
            Pattern = ParseExpression(ComparisonBindingPower + 1),
        };
    }

    private BetweenExpression ParseBetween(Expression target)
    {
        return new BetweenExpression
        {
            Target = target,
            BetweenKeyword = Advance(),
            Lower = ParseExpression(ComparisonBindingPower + 1),
            AndKeyword = Expect(SyntaxKind.AndKeyword),
            Upper = ParseExpression(ComparisonBindingPower + 1),
        };
    }

    private InExpression ParseIn(Expression target)
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        if (IsQueryStart(_current.Kind))
        {
            return new InExpression
            {
                Target = target,
                NotKeyword = notKeyword,
                InKeyword = inKeyword,
                OpenParen = openParen,
                Values = [],
                Query = ParseQuery(),
                CloseParen = Expect(SyntaxKind.CloseParen),
            };
        }

        var values = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            values.Add(ParseExpression());
        }

        return new InExpression
        {
            Target = target,
            NotKeyword = notKeyword,
            InKeyword = inKeyword,
            OpenParen = openParen,
            Values = values,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private IsNullExpression ParseIsNull(Expression target)
    {
        return new IsNullExpression
        {
            Target = target,
            IsKeyword = Expect(SyntaxKind.IsKeyword),
            NotKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null,
            NullKeyword = Expect(SyntaxKind.NullKeyword),
        };
    }

    private Expression ParsePrefix()
    {
        return _current.Kind switch
        {
            SyntaxKind.NotKeyword => ParseNot(),
            SyntaxKind.MinusToken or SyntaxKind.PlusToken => ParseUnary(),
            SyntaxKind.CaseKeyword => ParseCase(),
            SyntaxKind.CastKeyword => ParseCast(),
            SyntaxKind.ArrayKeyword => ParseArray(),
            SyntaxKind.Identifier => NextKind == SyntaxKind.OpenParen
                ? ParseFunctionCall()
                : new IdentifierExpression { Identifier = Advance() },
            SyntaxKind.Number or SyntaxKind.String => new LiteralExpression { Literal = Advance() },
            SyntaxKind.ExistsKeyword => ParseExists(),
            SyntaxKind.OpenParen => ParseParen(),
            _ => throw new SqlParseException($"Expected expression, found {_current.Kind}", _current.Position),
        };
    }

    private ArrayExpression ParseArray()
    {
        var arrayKeyword = Advance();
        var openBracket = Expect(SyntaxKind.OpenBracket);
        IReadOnlyList<Expression> elements = _current.Kind == SyntaxKind.CloseBracket
            ? []
            : ParseExpressionList();
        return new ArrayExpression
        {
            ArrayKeyword = arrayKeyword,
            OpenBracket = openBracket,
            Elements = elements,
            CloseBracket = Expect(SyntaxKind.CloseBracket),
        };
    }

    private CastExpression ParseCast()
    {
        return new CastExpression
        {
            CastKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Expression = ParseExpression(),
            AsKeyword = Expect(SyntaxKind.AsKeyword),
            Type = ParseDataType(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private DataType ParseDataType()
    {
        var name = Expect(SyntaxKind.Identifier);
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new DataType { Name = name };
        }

        var openParen = Advance();
        var precision = Expect(SyntaxKind.Number);
        SyntaxToken? scale = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            scale = Expect(SyntaxKind.Number);
        }

        return new DataType
        {
            Name = name,
            OpenParen = openParen,
            Precision = precision,
            Scale = scale,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private CaseExpression ParseCase()
    {
        var caseKeyword = Advance();
        var operand = _current.Kind == SyntaxKind.WhenKeyword ? null : ParseExpression();
        if (_current.Kind != SyntaxKind.WhenKeyword)
        {
            throw new SqlParseException("Expected WHEN", _current.Position);
        }

        var arms = new List<WhenClause> { ParseWhenClause() };
        while (_current.Kind == SyntaxKind.WhenKeyword)
        {
            arms.Add(ParseWhenClause());
        }

        SyntaxToken? elseKeyword = null;
        Expression? elseResult = null;
        if (_current.Kind == SyntaxKind.ElseKeyword)
        {
            elseKeyword = Advance();
            elseResult = ParseExpression();
        }

        return new CaseExpression
        {
            CaseKeyword = caseKeyword,
            Operand = operand,
            Arms = arms,
            ElseKeyword = elseKeyword,
            ElseResult = elseResult,
            EndKeyword = Expect(SyntaxKind.EndKeyword),
        };
    }

    private WhenClause ParseWhenClause()
    {
        return new WhenClause
        {
            WhenKeyword = Expect(SyntaxKind.WhenKeyword),
            Condition = ParseExpression(),
            ThenKeyword = Expect(SyntaxKind.ThenKeyword),
            Result = ParseExpression(),
        };
    }

    private UnaryExpression ParseUnary()
    {
        return new UnaryExpression
        {
            OperatorToken = Advance(),
            Expression = ParseExpression(UnaryBindingPower + 1),
        };
    }

    private NotExpression ParseNot()
    {
        return new NotExpression
        {
            NotKeyword = Advance(),
            Expression = ParseExpression(NotBindingPower + 1),
        };
    }

    private FunctionCallExpression ParseFunctionCall()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        IReadOnlyList<Expression> arguments;
        if (_current.Kind == SyntaxKind.CloseParen)
        {
            arguments = [];
        }
        else if (_current.Kind == SyntaxKind.Star)
        {
            arguments = [new StarExpression { Star = Advance() }];
        }
        else
        {
            arguments = ParseExpressionList();
        }

        return new FunctionCallExpression
        {
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private QuantifiedSubqueryExpression ParseQuantifiedSubquery(Expression left, SyntaxToken operatorToken)
    {
        return new QuantifiedSubqueryExpression
        {
            Left = left,
            OperatorToken = operatorToken,
            Quantifier = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Query = ParseQuery(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private ExistsExpression ParseExists()
    {
        return new ExistsExpression
        {
            ExistsKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Query = ParseQuery(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private Expression ParseParen()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        if (IsQueryStart(_current.Kind))
        {
            return new ScalarSubqueryExpression
            {
                OpenParen = openParen,
                Query = ParseQuery(),
                CloseParen = Expect(SyntaxKind.CloseParen),
            };
        }

        return new ParenExpression
        {
            OpenParen = openParen,
            Inner = ParseExpression(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private const int OrBindingPower = 1;
    private const int AndBindingPower = 2;
    private const int NotBindingPower = 3;
    private const int ComparisonBindingPower = 4;
    private const int AddBindingPower = 5;
    private const int MultiplyBindingPower = 6;
    private const int UnaryBindingPower = 7;
    private const int DotBindingPower = 8;
    private const int ColonCastBindingPower = 9;

    private SyntaxKind NextKind =>
        _index < _tokens.Count ? _tokens[_index].Kind : SyntaxKind.EndOfFile;

    private static int BindingPower(SyntaxKind kind) => kind switch
    {
        SyntaxKind.OrKeyword => OrBindingPower,
        SyntaxKind.AndKeyword => AndBindingPower,
        SyntaxKind.EqualsToken or SyntaxKind.NotEqualsToken or SyntaxKind.GreaterThan
            or SyntaxKind.GreaterOrEqual or SyntaxKind.LessThan or SyntaxKind.LessOrEqual
            => ComparisonBindingPower,
        SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.ConcatToken => AddBindingPower,
        SyntaxKind.Star or SyntaxKind.SlashToken => MultiplyBindingPower,
        _ => 0,
    };

    private SyntaxToken Advance()
    {
        var current = _current;
        if (_index < _tokens.Count)
        {
            _current = _tokens[_index];
            _index++;
        }

        return current;
    }

    private SyntaxToken Expect(SyntaxKind kind)
    {
        if (_current.Kind != kind)
        {
            throw new SqlParseException($"Expected {kind}, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }
}
