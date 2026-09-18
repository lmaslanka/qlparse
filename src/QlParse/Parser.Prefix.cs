namespace QlParse;

internal sealed partial class Parser
{
    private Expression ParsePrefix()
    {
        return _current.Kind switch
        {
            SyntaxKind.NotKeyword => ParseNot(),
            SyntaxKind.MinusToken or SyntaxKind.PlusToken => ParseUnary(),
            SyntaxKind.CaseKeyword => ParseCase(),
            SyntaxKind.CastKeyword => ParseCast(),
            SyntaxKind.ArrayKeyword => ParseArray(),
            SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword or SyntaxKind.TimestampKeyword
                => ParseDatetimeLiteral(),
            SyntaxKind.IntervalKeyword => ParseIntervalLiteral(),
            SyntaxKind.TrimKeyword => ParseTrim(),
            SyntaxKind.ExtractKeyword => ParseExtract(),
            SyntaxKind.SubstringKeyword => ParseSubstring(),
            SyntaxKind.PositionKeyword => ParsePosition(),
            SyntaxKind.ConvertKeyword or SyntaxKind.TranslateKeyword => ParseUsingTransform(),
            SyntaxKind.UserKeyword or SyntaxKind.CurrentDateKeyword or SyntaxKind.CurrentTimeKeyword
                or SyntaxKind.CurrentTimestampKeyword or SyntaxKind.CurrentUserKeyword
                or SyntaxKind.SessionUserKeyword or SyntaxKind.SystemUserKeyword
                => ParseNiladicFunction(),
            SyntaxKind.Identifier => NextKind == SyntaxKind.OpenParen
                ? ParseFunctionCall()
                : new IdentifierExpression { Identifier = Advance() },
            SyntaxKind.Number or SyntaxKind.String or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword
                or SyntaxKind.NullKeyword
                => new LiteralExpression { Literal = Advance() },
            SyntaxKind.ExistsKeyword => ParseExists(),
            SyntaxKind.UniqueKeyword => ParseUnique(),
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

    private SyntaxToken ParseTypeName()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword
            or SyntaxKind.TimestampKeyword or SyntaxKind.IntervalKeyword)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected type name, found {_current.Kind}", _current.Position);
    }

    private NiladicFunctionExpression ParseNiladicFunction()
    {
        var name = Advance();
        var allowsPrecision = name.Kind is SyntaxKind.CurrentTimeKeyword or SyntaxKind.CurrentTimestampKeyword;
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new NiladicFunctionExpression { Name = name };
        }

        if (!allowsPrecision)
        {
            throw new SqlParseException($"{NiladicName(name.Kind)} does not take parentheses", _current.Position);
        }

        var openParen = Advance();
        return new NiladicFunctionExpression
        {
            Name = name,
            OpenParen = openParen,
            Precision = Expect(SyntaxKind.Number),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private static string NiladicName(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UserKeyword => Keyword.UserUpper,
        SyntaxKind.CurrentDateKeyword => Keyword.CurrentDateUpper,
        SyntaxKind.CurrentTimeKeyword => Keyword.CurrentTimeUpper,
        SyntaxKind.CurrentTimestampKeyword => Keyword.CurrentTimestampUpper,
        SyntaxKind.CurrentUserKeyword => Keyword.CurrentUserUpper,
        SyntaxKind.SessionUserKeyword => Keyword.SessionUserUpper,
        SyntaxKind.SystemUserKeyword => Keyword.SystemUserUpper,
        _ => throw new InvalidOperationException($"Unknown niladic function {kind}"),
    };

    private DatetimeLiteralExpression ParseDatetimeLiteral()
    {
        return new DatetimeLiteralExpression
        {
            KindKeyword = Advance(),
            Literal = Expect(SyntaxKind.String),
        };
    }

    private IntervalLiteralExpression ParseIntervalLiteral()
    {
        var intervalKeyword = Advance();
        SyntaxToken? sign = null;
        if (_current.Kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken)
        {
            sign = Advance();
        }

        return new IntervalLiteralExpression
        {
            IntervalKeyword = intervalKeyword,
            Sign = sign,
            Literal = Expect(SyntaxKind.String),
            Qualifier = ParseIntervalQualifier(),
        };
    }

    private IntervalQualifier ParseIntervalQualifier()
    {
        var start = ParseIntervalField();
        if (!IdentifierEquals(Keyword.To))
        {
            return new IntervalQualifier { Start = start };
        }

        return new IntervalQualifier
        {
            Start = start,
            ToKeyword = Advance(),
            End = ParseIntervalField(),
        };
    }

    private IntervalField ParseIntervalField()
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            throw new SqlParseException($"Expected interval field, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new IntervalField { Name = name };
        }

        var openParen = Advance();
        var precision = Expect(SyntaxKind.Number);
        SyntaxToken? scale = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            scale = Expect(SyntaxKind.Number);
        }

        return new IntervalField
        {
            Name = name,
            OpenParen = openParen,
            Precision = precision,
            Scale = scale,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private bool IdentifierEquals(string keyword) => TokenEquals(_current, keyword);

    private bool TokenEquals(SyntaxToken token, string keyword) =>
        token.Kind == SyntaxKind.Identifier
        && token.TextOf(_source).Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);

    private bool IsTrimSpecification() =>
        IdentifierEquals(Keyword.Leading)
        || IdentifierEquals(Keyword.Trailing)
        || IdentifierEquals(Keyword.Both);

    private TrimExpression ParseTrim()
    {
        var trimKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        SyntaxToken? specification = null;
        Expression? characters = null;
        SyntaxToken? fromKeyword = null;
        Expression source;
        if (IsTrimSpecification())
        {
            specification = Advance();
            if (_current.Kind == SyntaxKind.FromKeyword)
            {
                fromKeyword = Advance();
                source = ParseExpression();
            }
            else
            {
                characters = ParseExpression();
                fromKeyword = Expect(SyntaxKind.FromKeyword);
                source = ParseExpression();
            }
        }
        else
        {
            var first = ParseExpression();
            if (_current.Kind == SyntaxKind.FromKeyword)
            {
                characters = first;
                fromKeyword = Advance();
                source = ParseExpression();
            }
            else
            {
                source = first;
            }
        }

        return new TrimExpression
        {
            TrimKeyword = trimKeyword,
            OpenParen = openParen,
            Specification = specification,
            Characters = characters,
            FromKeyword = fromKeyword,
            Source = source,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private UsingTransformExpression ParseUsingTransform()
    {
        return new UsingTransformExpression
        {
            FunctionKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Expression = ParseExpression(),
            UsingKeyword = Expect(SyntaxKind.UsingKeyword),
            Name = Expect(SyntaxKind.Identifier),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private ExtractExpression ParseExtract()
    {
        return new ExtractExpression
        {
            ExtractKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Field = Expect(SyntaxKind.Identifier),
            FromKeyword = Expect(SyntaxKind.FromKeyword),
            Source = ParseExpression(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private SubstringExpression ParseSubstring()
    {
        var substringKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var source = ParseExpression();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var start = ParseExpression();
        SyntaxToken? forKeyword = null;
        Expression? length = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            length = ParseExpression();
        }

        return new SubstringExpression
        {
            SubstringKeyword = substringKeyword,
            OpenParen = openParen,
            Source = source,
            FromKeyword = fromKeyword,
            Start = start,
            ForKeyword = forKeyword,
            Length = length,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private PositionExpression ParsePosition()
    {
        return new PositionExpression
        {
            PositionKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            Needle = ParseExpression(ComparisonBindingPower + 1),
            InKeyword = Expect(SyntaxKind.InKeyword),
            Haystack = ParseExpression(),
            CloseParen = Expect(SyntaxKind.CloseParen),
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
        var name = ParseTypeName();
        SyntaxToken? secondName = null;
        if (TokenEquals(name, Keyword.Double) && IdentifierEquals(Keyword.Precision))
        {
            secondName = Advance();
        }
        else if ((TokenEquals(name, Keyword.Character) || TokenEquals(name, Keyword.Char))
            && IdentifierEquals(Keyword.Varying))
        {
            secondName = Advance();
        }

        SyntaxToken? openParen = null;
        SyntaxToken? precision = null;
        SyntaxToken? scale = null;
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            precision = Expect(SyntaxKind.Number);
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                scale = Expect(SyntaxKind.Number);
            }

            closeParen = Expect(SyntaxKind.CloseParen);
        }

        SyntaxToken? withKeyword = null;
        SyntaxToken? timeKeyword = null;
        SyntaxToken? zone = null;
        if (name.Kind is SyntaxKind.TimeKeyword or SyntaxKind.TimestampKeyword
            && _current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            timeKeyword = Expect(SyntaxKind.TimeKeyword);
            if (!IdentifierEquals(Keyword.Zone))
            {
                throw new SqlParseException($"Expected ZONE, found {_current.Kind}", _current.Position);
            }

            zone = Advance();
        }

        return new DataType
        {
            Name = name,
            SecondName = secondName,
            OpenParen = openParen,
            Precision = precision,
            Scale = scale,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            TimeKeyword = timeKeyword,
            Zone = zone,
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

        var closeParen = Expect(SyntaxKind.CloseParen);
        FilterClause? filter = _current.Kind == SyntaxKind.FilterKeyword ? ParseFilter() : null;
        return new FunctionCallExpression
        {
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            Filter = filter,
        };
    }

    private FilterClause ParseFilter()
    {
        return new FilterClause
        {
            FilterKeyword = Advance(),
            OpenParen = Expect(SyntaxKind.OpenParen),
            WhereKeyword = Expect(SyntaxKind.WhereKeyword),
            Expression = ParseExpression(),
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

    private UniqueExpression ParseUnique()
    {
        return new UniqueExpression
        {
            UniqueKeyword = Advance(),
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

        var first = ParseExpression();
        if (_current.Kind != SyntaxKind.Comma)
        {
            return new ParenExpression
            {
                OpenParen = openParen,
                Inner = first,
                CloseParen = Expect(SyntaxKind.CloseParen),
            };
        }

        var elements = new List<Expression> { first };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseExpression());
        }

        return new RowConstructorExpression
        {
            OpenParen = openParen,
            Elements = elements,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private MatchExpression ParseMatch(Expression left)
    {
        return new MatchExpression
        {
            Left = left,
            MatchKeyword = Advance(),
            UniqueKeyword = _current.Kind == SyntaxKind.UniqueKeyword ? Advance() : null,
            MatchType = _current.Kind is SyntaxKind.PartialKeyword or SyntaxKind.FullKeyword
                ? Advance()
                : null,
            OpenParen = Expect(SyntaxKind.OpenParen),
            Query = ParseQuery(),
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private OverlapsExpression ParseOverlaps(Expression left)
    {
        return new OverlapsExpression
        {
            Left = left,
            OverlapsKeyword = Advance(),
            Right = ParseExpression(ComparisonBindingPower + 1),
        };
    }
}
