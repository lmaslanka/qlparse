namespace QlParse;

internal sealed partial class Parser
{
    private DataType ParseDataType()
    {
        if (IdentifierEquals(Keyword.Row) && NextKind == SyntaxKind.OpenParen)
        {
            return ParseRowType();
        }

        if (IdentifierEquals(Keyword.Ref) && NextKind == SyntaxKind.OpenParen)
        {
            return ParseRefType();
        }

        var name = ParseTypeName();
        if (TokenEquals(name, Keyword.Xml) && _current.Kind == SyntaxKind.OpenParen)
        {
            return ParseXmlType(name);
        }

        var nameTail = _current.Kind == SyntaxKind.Dot ? ParseDottedNameTail() : ParseNameTail(name);
        var precisionScale = ParseOptionalPrecisionScale();
        var timeZone = ParseOptionalTimeZone(name);
        var openParen = precisionScale.OpenParen;
        var precision = precisionScale.Precision;
        var scale = precisionScale.Scale;
        var closeParen = precisionScale.CloseParen;
        var withKeyword = timeZone.WithKeyword;
        var timeKeyword = timeZone.TimeKeyword;
        var zone = timeZone.Zone;
        var collections = ParseCollectionSuffixes();
        var endToken = ParseDataTypeEnd(name, nameTail, zone, closeParen, collections);
        return new DataType
        {
            Span = SourceSpan.From(name, endToken),
            Name = name,
            NameTail = nameTail,
            OpenParen = openParen,
            Precision = precision,
            Scale = scale,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            TimeKeyword = timeKeyword,
            Zone = zone,
            Collections = collections,
        };
    }

    private static SourceSpan ParseDataTypeEnd(
        SyntaxToken name,
        IReadOnlyList<SyntaxToken> nameTail,
        SyntaxToken? zone,
        SyntaxToken? closeParen,
        IReadOnlyList<CollectionSuffix> collections)
    {
        if (collections.Count > 0)
        {
            return (collections[^1].CloseBracket ?? collections[^1].Keyword).Span;
        }

        return zone?.Span ?? closeParen?.Span ?? (nameTail.Count > 0 ? nameTail[^1].Span : name.Span);
    }

    private readonly record struct PrecisionScale(SyntaxToken? OpenParen, SyntaxToken? Precision, SyntaxToken? Scale, SyntaxToken? CloseParen);

    private PrecisionScale ParseOptionalPrecisionScale()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new PrecisionScale(null, null, null, null);
        }

        var openParen = Advance();
        var precision = Expect(SyntaxKind.Number);
        SyntaxToken? scale = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            scale = Expect(SyntaxKind.Number);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PrecisionScale(openParen, precision, scale, closeParen);
    }

    private readonly record struct TimeZoneClause(SyntaxToken? WithKeyword, SyntaxToken? TimeKeyword, SyntaxToken? Zone);

    private TimeZoneClause ParseOptionalTimeZone(SyntaxToken name)
    {
        if (name.Kind is not (SyntaxKind.TimeKeyword or SyntaxKind.TimestampKeyword) || _current.Kind != SyntaxKind.WithKeyword)
        {
            return new TimeZoneClause(null, null, null);
        }

        var withKeyword = Advance();
        var timeKeyword = Expect(SyntaxKind.TimeKeyword);
        if (!IdentifierEquals(Keyword.Zone))
        {
            throw new SqlParseException($"Expected ZONE, found {_current.Kind}", _current.Position);
        }

        var zone = Advance();
        return new TimeZoneClause(withKeyword, timeKeyword, zone);
    }

    private DataType ParseXmlType(SyntaxToken name)
    {
        var openParen = Advance();
        if (!IdentifierEquals(Keyword.Document) && !IdentifierEquals(Keyword.Content) && !IdentifierEquals(Keyword.Sequence))
        {
            throw new SqlParseException($"Expected DOCUMENT, CONTENT, or SEQUENCE, found {_current.Kind}", _current.Position);
        }

        var modifier = Advance();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var collections = ParseCollectionSuffixes();
        var endToken = collections.Count > 0
            ? collections[^1].CloseBracket ?? collections[^1].Keyword
            : closeParen;
        return new DataType
        {
            Span = SourceSpan.From(name, endToken),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Modifier = modifier,
            Collections = collections,
        };
    }

    private DataType ParseRowType()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var fields = new List<FieldDefinition> { ParseFieldDefinition() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            fields.Add(ParseFieldDefinition());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var collections = ParseCollectionSuffixes();
        var endToken = collections.Count > 0
            ? collections[^1].CloseBracket ?? collections[^1].Keyword
            : closeParen;
        return new DataType
        {
            Span = SourceSpan.From(name, endToken),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Collections = collections,
            Fields = fields,
        };
    }

    private DataType ParseRefType()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var referencedType = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? scopeKeyword = null;
        IReadOnlyList<SyntaxToken>? scopeName = null;
        if (IdentifierEquals(Keyword.Scope))
        {
            scopeKeyword = Advance();
            var parts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
            while (_current.Kind == SyntaxKind.Dot)
            {
                Advance();
                parts.Add(Expect(SyntaxKind.Identifier));
            }

            scopeName = parts;
        }

        var collections = ParseCollectionSuffixes();
        var scopeNameOrCloseParen = scopeName is not null ? scopeName[^1] : closeParen;
        var endToken = collections.Count > 0
            ? collections[^1].CloseBracket ?? collections[^1].Keyword
            : scopeNameOrCloseParen;
        return new DataType
        {
            Span = SourceSpan.From(name, endToken),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Collections = collections,
            ReferencedType = referencedType,
            ScopeKeyword = scopeKeyword,
            ScopeName = scopeName,
        };
    }

    private FieldDefinition ParseFieldDefinition()
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            throw new SqlParseException($"Expected field name, found {_current.Kind}", _current.Position);
        }

        var fieldName = Advance();
        var type = ParseDataType();
        return new FieldDefinition
        {
            Span = SourceSpan.From(fieldName, type.Span),
            Name = fieldName,
            Type = type,
        };
    }

    private List<CollectionSuffix> ParseCollectionSuffixes()
    {
        var suffixes = new List<CollectionSuffix>();
        while (true)
        {
            if (_current.Kind == SyntaxKind.ArrayKeyword)
            {
                suffixes.Add(ParseArraySuffix());
                continue;
            }

            if (_current.Kind == SyntaxKind.MultisetKeyword)
            {
                var keyword = Advance();
                suffixes.Add(new CollectionSuffix { Span = keyword.Span, Keyword = keyword });
                continue;
            }

            if (IdentifierEquals(Keyword.Mdarray))
            {
                suffixes.Add(ParseMdarraySuffix());
                continue;
            }

            break;
        }

        return suffixes;
    }

    private CollectionSuffix ParseArraySuffix()
    {
        var keyword = Advance();
        SyntaxToken? openBracket = null;
        SyntaxToken? cardinality = null;
        SyntaxToken? closeBracket = null;
        if (_current.Kind == SyntaxKind.OpenBracket)
        {
            openBracket = Advance();
            cardinality = Expect(SyntaxKind.Number);
            closeBracket = Expect(SyntaxKind.CloseBracket);
        }

        var end = closeBracket ?? keyword;
        return new CollectionSuffix
        {
            Span = SourceSpan.From(keyword, end),
            Keyword = keyword,
            OpenBracket = openBracket,
            Cardinality = cardinality,
            CloseBracket = closeBracket,
        };
    }

    private CollectionSuffix ParseMdarraySuffix()
    {
        var keyword = Advance();
        var openBracket = Expect(SyntaxKind.OpenBracket);
        var dimensions = new List<MdarrayDimension> { ParseMdarrayDimension() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            dimensions.Add(ParseMdarrayDimension());
        }

        var closeBracket = Expect(SyntaxKind.CloseBracket);
        return new CollectionSuffix
        {
            Span = SourceSpan.From(keyword, closeBracket),
            Keyword = keyword,
            OpenBracket = openBracket,
            CloseBracket = closeBracket,
            Dimensions = dimensions,
        };
    }

    private MdarrayDimension ParseMdarrayDimension()
    {
        var first = Expect(SyntaxKind.Number);
        if (_current.Kind != SyntaxKind.ColonToken)
        {
            return new MdarrayDimension { Span = first.Span, Upper = first };
        }

        var colon = Advance();
        var upper = Expect(SyntaxKind.Number);
        return new MdarrayDimension
        {
            Span = SourceSpan.From(first, upper),
            Lower = first,
            Colon = colon,
            Upper = upper,
        };
    }

    private List<SyntaxToken> ParseDottedNameTail()
    {
        var parts = new List<SyntaxToken>();
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            parts.Add(Expect(SyntaxKind.Identifier));
        }

        return parts;
    }

    private List<SyntaxToken> ParseNameTail(SyntaxToken name)
    {
        if (TokenEquals(name, Keyword.Double) && IdentifierEquals(Keyword.Precision))
        {
            return [Advance()];
        }

        if (TokenEquals(name, Keyword.Character) || TokenEquals(name, Keyword.Char)
            || TokenEquals(name, Keyword.Nchar) || TokenEquals(name, Keyword.Binary))
        {
            return ParseVaryingOrLargeObject();
        }

        if (TokenEquals(name, Keyword.National)
            && (IdentifierEquals(Keyword.Char) || IdentifierEquals(Keyword.Character)))
        {
            var nationalTail = new List<SyntaxToken> { Advance() };
            nationalTail.AddRange(ParseVaryingOrLargeObject());
            return nationalTail;
        }

        return [];
    }

    private List<SyntaxToken> ParseVaryingOrLargeObject()
    {
        if (IdentifierEquals(Keyword.Varying))
        {
            return [Advance()];
        }

        if (IdentifierEquals(Keyword.Large))
        {
            var large = Advance();
            if (!IdentifierEquals(Keyword.Object))
            {
                throw new SqlParseException($"Expected OBJECT, found {_current.Kind}", _current.Position);
            }

            return [large, Advance()];
        }

        return [];
    }

    internal CaseExpression ParseCase()
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

        var endKeyword = Expect(SyntaxKind.EndKeyword);
        return new CaseExpression
        {
            Span = SourceSpan.From(caseKeyword, endKeyword),
            CaseKeyword = caseKeyword,
            Operand = operand,
            Arms = arms,
            ElseKeyword = elseKeyword,
            ElseResult = elseResult,
            EndKeyword = endKeyword,
        };
    }

    private WhenClause ParseWhenClause()
    {
        var whenKeyword = Expect(SyntaxKind.WhenKeyword);
        var condition = ParseExpression();
        var thenKeyword = Expect(SyntaxKind.ThenKeyword);
        var result = ParseExpression();
        return new WhenClause
        {
            Span = SourceSpan.From(whenKeyword, result.Span),
            WhenKeyword = whenKeyword,
            Condition = condition,
            ThenKeyword = thenKeyword,
            Result = result,
        };
    }

    internal UnaryExpression ParseUnary()
    {
        var operatorToken = Advance();
        var expression = ParseExpression(UnaryBindingPower + 1);
        return new UnaryExpression
        {
            Span = SourceSpan.From(operatorToken, expression.Span),
            OperatorToken = operatorToken,
            Expression = expression,
        };
    }

    internal NotExpression ParseNot()
    {
        var notKeyword = Advance();
        var expression = ParseExpression(NotBindingPower + 1);
        return new NotExpression
        {
            Span = SourceSpan.From(notKeyword, expression.Span),
            NotKeyword = notKeyword,
            Expression = expression,
        };
    }

    internal FunctionCallExpression ParseFunctionCall()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = ParseFunctionCallArguments();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? fromKeyword = null;
        SyntaxToken? fromPosition = null;
        if (_current.Kind == SyntaxKind.FromKeyword
            && (NextIs(Keyword.First) || NextIs(Keyword.Last)))
        {
            fromKeyword = Advance();
            fromPosition = Advance();
        }

        var nullTreatment = ParseOptionalNullTreatment();
        var nullTreatmentToken = nullTreatment.NullTreatment;
        var nullsKeyword = nullTreatment.NullsKeyword;
        FilterClause? filter = _current.Kind == SyntaxKind.FilterKeyword ? ParseFilter() : null;
        WindowSpecification? over = _current.Kind == SyntaxKind.OverKeyword ? ParseOver() : null;
        var end = over?.Span ?? filter?.Span ?? nullsKeyword?.Span ?? fromPosition?.Span ?? closeParen.Span;
        return new FunctionCallExpression
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            FromKeyword = fromKeyword,
            FromPosition = fromPosition,
            NullTreatment = nullTreatmentToken,
            NullsKeyword = nullsKeyword,
            Filter = filter,
            Over = over,
        };
    }

    private IReadOnlyList<Expression> ParseFunctionCallArguments()
    {
        if (_current.Kind == SyntaxKind.CloseParen)
        {
            return [];
        }

        if (_current.Kind == SyntaxKind.Star)
        {
            return [ParseStar()];
        }

        return ParseExpressionList();
    }

    private readonly record struct NullTreatmentClause(SyntaxToken? NullTreatment, SyntaxToken? NullsKeyword);

    private NullTreatmentClause ParseOptionalNullTreatment()
    {
        if (!IdentifierEquals(Keyword.Respect) && !IdentifierEquals(Keyword.Ignore))
        {
            return new NullTreatmentClause(null, null);
        }

        var nullTreatment = Advance();
        if (!IdentifierEquals(Keyword.Nulls))
        {
            throw new SqlParseException($"Expected NULLS, found {_current.Kind}", _current.Position);
        }

        return new NullTreatmentClause(nullTreatment, Advance());
    }

    private bool NextIs(string keyword) =>
        NextKind == SyntaxKind.Identifier
        && _index < _tokens.Count
        && TokenEquals(_tokens[_index], keyword);

    private FilterClause ParseFilter()
    {
        var filterKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var whereKeyword = Expect(SyntaxKind.WhereKeyword);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new FilterClause
        {
            Span = SourceSpan.From(filterKeyword, closeParen),
            FilterKeyword = filterKeyword,
            OpenParen = openParen,
            WhereKeyword = whereKeyword,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    internal QuantifiedSubqueryExpression ParseQuantifiedSubquery(Expression left, SyntaxToken operatorToken)
    {
        var quantifier = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new QuantifiedSubqueryExpression
        {
            Span = SourceSpan.From(left.Span, closeParen),
            Left = left,
            OperatorToken = operatorToken,
            Quantifier = quantifier,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    internal ExistsExpression ParseExists()
    {
        var existsKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ExistsExpression
        {
            Span = SourceSpan.From(existsKeyword, closeParen),
            ExistsKeyword = existsKeyword,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    internal UniqueExpression ParseUnique()
    {
        var uniqueKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new UniqueExpression
        {
            Span = SourceSpan.From(uniqueKeyword, closeParen),
            UniqueKeyword = uniqueKeyword,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    internal Expression ParseParen()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        if (IsQueryStart(_current.Kind))
        {
            var query = ParseQuery();
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new ScalarSubqueryExpression
            {
                Span = SourceSpan.From(openParen, closeParen),
                OpenParen = openParen,
                Query = query,
                CloseParen = closeParen,
            };
        }

        var first = ParseExpression();
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            return ParseGeneralizedInvocation(openParen, first);
        }

        if (_current.Kind != SyntaxKind.Comma)
        {
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new ParenExpression
            {
                Span = SourceSpan.From(openParen, closeParen),
                OpenParen = openParen,
                Inner = first,
                CloseParen = closeParen,
            };
        }

        var elements = new List<Expression> { first };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseExpression());
        }

        var close = Expect(SyntaxKind.CloseParen);
        return new RowConstructorExpression
        {
            Span = SourceSpan.From(openParen, close),
            OpenParen = openParen,
            Elements = elements,
            CloseParen = close,
        };
    }

    private MethodInvocationExpression ParseGeneralizedInvocation(SyntaxToken openParen, Expression target)
    {
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var type = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var dot = Expect(SyntaxKind.Dot);
        var name = Expect(SyntaxKind.Identifier);
        ParseParenthesizedArguments(out var argumentsOpen, out var arguments, out var argumentsClose);
        return new MethodInvocationExpression
        {
            Span = SourceSpan.From(openParen, argumentsClose),
            OpenParen = openParen,
            Target = target,
            AsKeyword = asKeyword,
            Type = type,
            CloseParen = closeParen,
            Dot = dot,
            Name = name,
            ArgumentsOpen = argumentsOpen,
            Arguments = arguments,
            ArgumentsClose = argumentsClose,
        };
    }

    internal MatchExpression ParseMatch(Expression left)
    {
        var matchKeyword = Advance();
        var uniqueKeyword = _current.Kind == SyntaxKind.UniqueKeyword ? Advance() : (SyntaxToken?)null;
        var matchType = _current.Kind is SyntaxKind.PartialKeyword or SyntaxKind.FullKeyword
            ? Advance()
            : (SyntaxToken?)null;
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MatchExpression
        {
            Span = SourceSpan.From(left.Span, closeParen),
            Left = left,
            MatchKeyword = matchKeyword,
            UniqueKeyword = uniqueKeyword,
            MatchType = matchType,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    internal OverlapsExpression ParseOverlaps(Expression left)
    {
        var overlapsKeyword = Advance();
        var right = ParseExpression(ComparisonBindingPower + 1);
        return new OverlapsExpression
        {
            Span = SourceSpan.From(left.Span, right.Span),
            Left = left,
            OverlapsKeyword = overlapsKeyword,
            Right = right,
        };
    }
}
