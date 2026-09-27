namespace QlParse;

internal sealed partial class Parser
{
    internal Expression ParsePrefix()
    {
        return TryParsePrefixHead()
            ?? TryParsePrefixFunctions()
            ?? ParsePrefixTail();
    }

    private Expression? TryParsePrefixHead() => TryParsePrefixHeadA() ?? TryParsePrefixHeadB() ?? TryParsePrefixHeadC();

    private Expression? TryParsePrefixHeadA()
    {
        return _current.Kind switch
        {
            SyntaxKind.NotKeyword => ParseNot(),
            SyntaxKind.MinusToken or SyntaxKind.PlusToken => ParseUnary(),
            SyntaxKind.CaseKeyword => ParseCase(),
            SyntaxKind.CastKeyword => ParseCast(),
            SyntaxKind.TreatKeyword => ParseTreat(),
            SyntaxKind.DerefKeyword => ParseDeref(),
            SyntaxKind.SpecifictypeKeyword => ParseSpecifictype(),
            _ => null,
        };
    }

    private Expression? TryParsePrefixHeadB()
    {
        return _current.Kind switch
        {
            SyntaxKind.NullIfKeyword => ParseNullIf(),
            SyntaxKind.CoalesceKeyword => ParseCoalesce(),
            SyntaxKind.ArrayKeyword => NextKind == SyntaxKind.OpenParen ? ParseArrayQuery() : ParseArray(),
            SyntaxKind.MultisetKeyword => NextKind == SyntaxKind.OpenParen ? ParseMultisetQuery() : ParseMultiset(),
            SyntaxKind.SetKeyword => ParseMultisetSet(),
            _ => null,
        };
    }

    private Expression? TryParsePrefixHeadC()
    {
        return _current.Kind switch
        {
            SyntaxKind.CardinalityKeyword or SyntaxKind.ElementKeyword => ParseCollectionFunction(),
            SyntaxKind.AbsentKeyword => ParseAbsentOnNull(),
            SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword or SyntaxKind.TimestampKeyword
                => ParseDatetimeLiteral(),
            SyntaxKind.IntervalKeyword => ParseIntervalLiteral(),
            SyntaxKind.TrimKeyword => ParseTrim(),
            SyntaxKind.ExtractKeyword => ParseExtract(),
            SyntaxKind.SubstringKeyword => ParseSubstring(),
            SyntaxKind.PositionKeyword => ParsePosition(),
            _ => null,
        };
    }

    private Expression? TryParsePrefixFunctions()
    {
        return _current.Kind switch
        {
            SyntaxKind.UpperKeyword or SyntaxKind.LowerKeyword or SyntaxKind.CharLengthKeyword
                or SyntaxKind.CharacterLengthKeyword or SyntaxKind.OctetLengthKeyword
                or SyntaxKind.BitLengthKeyword or SyntaxKind.NormalizeKeyword
                or SyntaxKind.FloorKeyword or SyntaxKind.CeilKeyword or SyntaxKind.CeilingKeyword
                or SyntaxKind.PowerKeyword or SyntaxKind.SqrtKeyword or SyntaxKind.LnKeyword
                or SyntaxKind.ExpKeyword or SyntaxKind.ModKeyword or SyntaxKind.AbsKeyword
                or SyntaxKind.WidthBucketKeyword or SyntaxKind.GroupingKeyword => ParseSpecialForm(),
            SyntaxKind.OverlayKeyword => ParseOverlay(),
            SyntaxKind.ConvertKeyword or SyntaxKind.TranslateKeyword => ParseUsingTransform(),
            SyntaxKind.UserKeyword or SyntaxKind.CurrentDateKeyword or SyntaxKind.CurrentTimeKeyword
                or SyntaxKind.CurrentTimestampKeyword or SyntaxKind.CurrentUserKeyword
                or SyntaxKind.SessionUserKeyword or SyntaxKind.SystemUserKeyword
                or SyntaxKind.LocalTimeKeyword or SyntaxKind.LocalTimestampKeyword
                or SyntaxKind.CurrentRoleKeyword or SyntaxKind.CurrentCatalogKeyword
                or SyntaxKind.CurrentSchemaKeyword or SyntaxKind.CurrentPathKeyword
                => ParseNiladicFunction(),
            _ => null,
        };
    }

    private Expression ParsePrefixTail()
    {
        return _current.Kind switch
        {
            SyntaxKind.Identifier => ParseIdentifierPrefix(),
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

    internal SyntaxToken ParseTypeName()
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
        var allowsPrecision = name.Kind is SyntaxKind.CurrentTimeKeyword or SyntaxKind.CurrentTimestampKeyword
            or SyntaxKind.LocalTimeKeyword or SyntaxKind.LocalTimestampKeyword;
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

    private static string NiladicName(SyntaxKind kind) =>
        TryNiladicNameHead(kind)
        ?? TryNiladicNameTail(kind)
        ?? throw new InvalidOperationException($"Unknown niladic function {kind}");

    private static string? TryNiladicNameHead(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UserKeyword => Keyword.UserUpper,
        SyntaxKind.CurrentDateKeyword => Keyword.CurrentDateUpper,
        SyntaxKind.CurrentTimeKeyword => Keyword.CurrentTimeUpper,
        SyntaxKind.CurrentTimestampKeyword => Keyword.CurrentTimestampUpper,
        SyntaxKind.CurrentUserKeyword => Keyword.CurrentUserUpper,
        _ => null,
    };

    private static string? TryNiladicNameTail(SyntaxKind kind) => kind switch
    {
        SyntaxKind.SessionUserKeyword => Keyword.SessionUserUpper,
        SyntaxKind.SystemUserKeyword => Keyword.SystemUserUpper,
        SyntaxKind.CurrentRoleKeyword => Keyword.CurrentRoleUpper,
        SyntaxKind.CurrentCatalogKeyword => Keyword.CurrentCatalogUpper,
        SyntaxKind.CurrentSchemaKeyword => Keyword.CurrentSchemaUpper,
        SyntaxKind.CurrentPathKeyword => Keyword.CurrentPathUpper,
        _ => null,
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
            if (_current.Kind is SyntaxKind.FromKeyword)
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
        var fieldToken = Expect(SyntaxKind.Identifier);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var source = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ExtractExpression
        {
            Span = SourceSpan.From(extractKeyword, closeParen),
            ExtractKeyword = extractKeyword,
            OpenParen = openParen,
            Field = fieldToken,
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

    private SpecialFormExpression ParseSpecialForm()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = new List<Expression> { ParseExpression() };
        SyntaxToken? usingKeyword = null;
        SyntaxToken? usingName = null;
        if (name.Kind is SyntaxKind.CharLengthKeyword or SyntaxKind.CharacterLengthKeyword
            && _current.Kind == SyntaxKind.UsingKeyword)
        {
            usingKeyword = Advance();
            usingName = Expect(SyntaxKind.Identifier);
        }
        else
        {
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParseExpression());
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new SpecialFormExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            UsingKeyword = usingKeyword,
            UsingName = usingName,
            CloseParen = closeParen,
        };
    }

    private OverlayExpression ParseOverlay()
    {
        var overlayKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var source = ParseExpression();
        if (!IdentifierEquals(Keyword.Placing))
        {
            throw new SqlParseException($"Expected PLACING, found {_current.Kind}", _current.Position);
        }

        var placingKeyword = Advance();
        var replacement = ParseExpression();
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
        return new OverlayExpression
        {
            Span = SourceSpan.From(overlayKeyword, closeParen),
            OverlayKeyword = overlayKeyword,
            OpenParen = openParen,
            Source = source,
            PlacingKeyword = placingKeyword,
            Replacement = replacement,
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

    private Expression ParseIdentifierPrefix()
    {
        if (IsNextValueFor())
        {
            return ParseNextValue();
        }

        if (IsRefValue())
        {
            return ParseRefValue();
        }

        if (IsNewSpecification())
        {
            return ParseNewSpecification();
        }

        if (IsPeriodValue())
        {
            return ParsePeriodValue();
        }

        if (IsMarkupCall())
        {
            return ParseMarkupCall();
        }

        if (IsMdarrayConstructor())
        {
            return ParseMdarrayConstructor();
        }

        if (IsMdarrayAggregate())
        {
            return ParseMdarrayAggregate();
        }

        if (IsTableConstructor())
        {
            return ParseMultisetQuery();
        }

        return NextKind == SyntaxKind.OpenParen ? ParseFunctionCall() : Identifier();
    }

    private bool IsRefValue() =>
        IdentifierEquals(Keyword.Ref) && NextKind == SyntaxKind.OpenParen;

    private bool IsNewSpecification()
    {
        if (!IdentifierEquals(Keyword.New) || NextKind != SyntaxKind.Identifier)
        {
            return false;
        }

        var index = _index + 1;
        while (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.Dot)
        {
            index++;
            if (index >= _tokens.Count || _tokens[index].Kind != SyntaxKind.Identifier)
            {
                return false;
            }

            index++;
        }

        return index < _tokens.Count && _tokens[index].Kind == SyntaxKind.OpenParen;
    }

    private bool IsPeriodValue() =>
        IdentifierEquals(Keyword.Period) && NextKind == SyntaxKind.OpenParen;

    private PeriodExpression ParsePeriodValue()
    {
        var periodKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var start = ParseExpression();
        var comma = Expect(SyntaxKind.Comma);
        var end = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PeriodExpression
        {
            Span = SourceSpan.From(periodKeyword, closeParen),
            PeriodKeyword = periodKeyword,
            OpenParen = openParen,
            Start = start,
            Comma = comma,
            End = end,
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

    private DerefExpression ParseDeref()
    {
        var derefKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new DerefExpression
        {
            Span = SourceSpan.From(derefKeyword, closeParen),
            DerefKeyword = derefKeyword,
            OpenParen = openParen,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    private RefValueExpression ParseRefValue()
    {
        var refKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new RefValueExpression
        {
            Span = SourceSpan.From(refKeyword, closeParen),
            RefKeyword = refKeyword,
            OpenParen = openParen,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    private SpecifictypeExpression ParseSpecifictype()
    {
        var specifictypeKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new SpecifictypeExpression
        {
            Span = SourceSpan.From(specifictypeKeyword, closeParen),
            SpecifictypeKeyword = specifictypeKeyword,
            OpenParen = openParen,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    private NewSpecificationExpression ParseNewSpecification()
    {
        var newKeyword = Advance();
        var typeName = ParseQualifiedName();
        ParseParenthesizedArguments(out var openParen, out var arguments, out var closeParen);
        return new NewSpecificationExpression
        {
            Span = SourceSpan.From(newKeyword, closeParen),
            NewKeyword = newKeyword,
            TypeName = typeName,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    private void ParseParenthesizedArguments(
        out SyntaxToken openParen,
        out IReadOnlyList<Expression> arguments,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
        closeParen = Expect(SyntaxKind.CloseParen);
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

}
