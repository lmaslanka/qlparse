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
            SyntaxKind.TreatKeyword => ParseTreat(),
            SyntaxKind.NullIfKeyword => ParseNullIf(),
            SyntaxKind.CoalesceKeyword => ParseCoalesce(),
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
            SyntaxKind.Identifier => IsNextValueFor()
                ? ParseNextValue()
                : NextKind == SyntaxKind.OpenParen
                    ? ParseFunctionCall()
                    : Identifier(),
            SyntaxKind.QuestionMark => HostParameter(),
            SyntaxKind.EmbeddedHost => EmbeddedHost(),
            SyntaxKind.Number or SyntaxKind.String or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword
                or SyntaxKind.NullKeyword
                => Literal(),
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
        var closeBracket = Expect(SyntaxKind.CloseBracket);
        return new ArrayExpression
        {
            Span = SourceSpan.From(arrayKeyword, closeBracket),
            ArrayKeyword = arrayKeyword,
            OpenBracket = openBracket,
            Elements = elements,
            CloseBracket = closeBracket,
        };
    }

    private IdentifierExpression Identifier()
    {
        var identifier = Advance();
        return new IdentifierExpression { Identifier = identifier, Span = identifier.Span };
    }

    private HostParameterExpression HostParameter()
    {
        var questionMark = Advance();
        return new HostParameterExpression { QuestionMark = questionMark, Span = questionMark.Span };
    }

    private EmbeddedHostExpression EmbeddedHost()
    {
        var name = Advance();
        return new EmbeddedHostExpression { Name = name, Span = name.Span };
    }

    private LiteralExpression Literal()
    {
        var literal = Advance();
        return new LiteralExpression { Literal = literal, Span = literal.Span };
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
            return new NiladicFunctionExpression { Name = name, Span = name.Span };
        }

        if (!allowsPrecision)
        {
            throw new SqlParseException($"{NiladicName(name.Kind)} does not take parentheses", _current.Position);
        }

        var openParen = Advance();
        var precision = Expect(SyntaxKind.Number);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new NiladicFunctionExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Precision = precision,
            CloseParen = closeParen,
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
        var kindKeyword = Advance();
        var literal = Expect(SyntaxKind.String);
        return new DatetimeLiteralExpression
        {
            Span = SourceSpan.From(kindKeyword, literal),
            KindKeyword = kindKeyword,
            Literal = literal,
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

        var literal = Expect(SyntaxKind.String);
        var qualifier = ParseIntervalQualifier();
        return new IntervalLiteralExpression
        {
            Span = SourceSpan.From(intervalKeyword, qualifier.Span),
            IntervalKeyword = intervalKeyword,
            Sign = sign,
            Literal = literal,
            Qualifier = qualifier,
        };
    }

    private IntervalQualifier ParseIntervalQualifier()
    {
        var start = ParseIntervalField();
        if (!IdentifierEquals(Keyword.To))
        {
            return new IntervalQualifier { Start = start, Span = start.Span };
        }

        var toKeyword = Advance();
        var end = ParseIntervalField();
        return new IntervalQualifier
        {
            Span = SourceSpan.From(start.Span, end.Span),
            Start = start,
            ToKeyword = toKeyword,
            End = end,
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
            return new IntervalField { Name = name, Span = name.Span };
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
        return new IntervalField
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Precision = precision,
            Scale = scale,
            CloseParen = closeParen,
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

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TrimExpression
        {
            Span = SourceSpan.From(trimKeyword, closeParen),
            TrimKeyword = trimKeyword,
            OpenParen = openParen,
            Specification = specification,
            Characters = characters,
            FromKeyword = fromKeyword,
            Source = source,
            CloseParen = closeParen,
        };
    }

    private UsingTransformExpression ParseUsingTransform()
    {
        var functionKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var usingKeyword = Expect(SyntaxKind.UsingKeyword);
        var name = Expect(SyntaxKind.Identifier);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new UsingTransformExpression
        {
            Span = SourceSpan.From(functionKeyword, closeParen),
            FunctionKeyword = functionKeyword,
            OpenParen = openParen,
            Expression = expression,
            UsingKeyword = usingKeyword,
            Name = name,
            CloseParen = closeParen,
        };
    }

    private ExtractExpression ParseExtract()
    {
        var extractKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var field = Expect(SyntaxKind.Identifier);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var source = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ExtractExpression
        {
            Span = SourceSpan.From(extractKeyword, closeParen),
            ExtractKeyword = extractKeyword,
            OpenParen = openParen,
            Field = field,
            FromKeyword = fromKeyword,
            Source = source,
            CloseParen = closeParen,
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

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new SubstringExpression
        {
            Span = SourceSpan.From(substringKeyword, closeParen),
            SubstringKeyword = substringKeyword,
            OpenParen = openParen,
            Source = source,
            FromKeyword = fromKeyword,
            Start = start,
            ForKeyword = forKeyword,
            Length = length,
            CloseParen = closeParen,
        };
    }

    private PositionExpression ParsePosition()
    {
        var positionKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var needle = ParseExpression(ComparisonBindingPower + 1);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var haystack = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PositionExpression
        {
            Span = SourceSpan.From(positionKeyword, closeParen),
            PositionKeyword = positionKeyword,
            OpenParen = openParen,
            Needle = needle,
            InKeyword = inKeyword,
            Haystack = haystack,
            CloseParen = closeParen,
        };
    }

    private CastExpression ParseCast()
    {
        var castKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var type = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new CastExpression
        {
            Span = SourceSpan.From(castKeyword, closeParen),
            CastKeyword = castKeyword,
            OpenParen = openParen,
            Expression = expression,
            AsKeyword = asKeyword,
            Type = type,
            CloseParen = closeParen,
        };
    }

    private bool IsNextValueFor() =>
        IdentifierEquals(Keyword.Next)
        && _index + 1 < _tokens.Count
        && TokenEquals(_tokens[_index], Keyword.Value)
        && _tokens[_index + 1].Kind == SyntaxKind.ForKeyword;

    private NextValueExpression ParseNextValue()
    {
        var nextKeyword = Advance();
        var valueKeyword = Advance();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var nameParts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        nameParts.AddRange(ParseDottedNameTail());
        return new NextValueExpression
        {
            Span = SourceSpan.From(nextKeyword, nameParts[^1]),
            NextKeyword = nextKeyword,
            ValueKeyword = valueKeyword,
            ForKeyword = forKeyword,
            NameParts = nameParts,
        };
    }

    private TreatExpression ParseTreat()
    {
        var treatKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var type = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TreatExpression
        {
            Span = SourceSpan.From(treatKeyword, closeParen),
            TreatKeyword = treatKeyword,
            OpenParen = openParen,
            Expression = expression,
            AsKeyword = asKeyword,
            Type = type,
            CloseParen = closeParen,
        };
    }

    private NullIfExpression ParseNullIf()
    {
        var nullIfKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var first = ParseExpression();
        Expect(SyntaxKind.Comma);
        var second = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new NullIfExpression
        {
            Span = SourceSpan.From(nullIfKeyword, closeParen),
            NullIfKeyword = nullIfKeyword,
            OpenParen = openParen,
            First = first,
            Second = second,
            CloseParen = closeParen,
        };
    }

    private CoalesceExpression ParseCoalesce()
    {
        var coalesceKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var first = ParseExpression();
        Expect(SyntaxKind.Comma);
        var arguments = new List<Expression> { first };
        arguments.AddRange(ParseExpressionList());
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new CoalesceExpression
        {
            Span = SourceSpan.From(coalesceKeyword, closeParen),
            CoalesceKeyword = coalesceKeyword,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

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
        var nameTail = _current.Kind == SyntaxKind.Dot ? ParseDottedNameTail() : ParseNameTail(name);

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

        var collections = ParseCollectionSuffixes();
        var endToken = collections.Count > 0
            ? collections[^1].CloseBracket ?? collections[^1].Keyword
            : zone ?? closeParen ?? (nameTail.Count > 0 ? nameTail[^1] : name);
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
        var endToken = collections.Count > 0
            ? collections[^1].CloseBracket ?? collections[^1].Keyword
            : scopeName is not null ? scopeName[^1] : closeParen;
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
            }
            else if (IdentifierEquals(Keyword.Multiset))
            {
                var keyword = Advance();
                suffixes.Add(new CollectionSuffix { Span = keyword.Span, Keyword = keyword });
            }
            else if (IdentifierEquals(Keyword.Mdarray))
            {
                suffixes.Add(ParseMdarraySuffix());
            }
            else
            {
                break;
            }
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

    private UnaryExpression ParseUnary()
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

    private NotExpression ParseNot()
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
            arguments = [ParseStar()];
        }
        else
        {
            arguments = ParseExpressionList();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        FilterClause? filter = _current.Kind == SyntaxKind.FilterKeyword ? ParseFilter() : null;
        return new FunctionCallExpression
        {
            Span = SourceSpan.From(name, filter?.Span ?? closeParen.Span),
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            Filter = filter,
        };
    }

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

    private QuantifiedSubqueryExpression ParseQuantifiedSubquery(Expression left, SyntaxToken operatorToken)
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

    private ExistsExpression ParseExists()
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

    private UniqueExpression ParseUnique()
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

    private Expression ParseParen()
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

    private MatchExpression ParseMatch(Expression left)
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

    private OverlapsExpression ParseOverlaps(Expression left)
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
