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
            SyntaxKind.DerefKeyword => ParseDeref(),
            SyntaxKind.SpecifictypeKeyword => ParseSpecifictype(),
            SyntaxKind.NullIfKeyword => ParseNullIf(),
            SyntaxKind.CoalesceKeyword => ParseCoalesce(),
            SyntaxKind.ArrayKeyword => NextKind == SyntaxKind.OpenParen ? ParseArrayQuery() : ParseArray(),
            SyntaxKind.MultisetKeyword => NextKind == SyntaxKind.OpenParen ? ParseMultisetQuery() : ParseMultiset(),
            SyntaxKind.SetKeyword => ParseMultisetSet(),
            SyntaxKind.CardinalityKeyword or SyntaxKind.ElementKeyword => ParseCollectionFunction(),
            SyntaxKind.AbsentKeyword => ParseAbsentOnNull(),
            SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword or SyntaxKind.TimestampKeyword
                => ParseDatetimeLiteral(),
            SyntaxKind.IntervalKeyword => ParseIntervalLiteral(),
            SyntaxKind.TrimKeyword => ParseTrim(),
            SyntaxKind.ExtractKeyword => ParseExtract(),
            SyntaxKind.SubstringKeyword => ParseSubstring(),
            SyntaxKind.PositionKeyword => ParsePosition(),
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

    private static string NiladicName(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UserKeyword => Keyword.UserUpper,
        SyntaxKind.CurrentDateKeyword => Keyword.CurrentDateUpper,
        SyntaxKind.CurrentTimeKeyword => Keyword.CurrentTimeUpper,
        SyntaxKind.CurrentTimestampKeyword => Keyword.CurrentTimestampUpper,
        SyntaxKind.CurrentUserKeyword => Keyword.CurrentUserUpper,
        SyntaxKind.SessionUserKeyword => Keyword.SessionUserUpper,
        SyntaxKind.SystemUserKeyword => Keyword.SystemUserUpper,
        SyntaxKind.CurrentRoleKeyword => Keyword.CurrentRoleUpper,
        SyntaxKind.CurrentCatalogKeyword => Keyword.CurrentCatalogUpper,
        SyntaxKind.CurrentSchemaKeyword => Keyword.CurrentSchemaUpper,
        SyntaxKind.CurrentPathKeyword => Keyword.CurrentPathUpper,
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
            else if (_current.Kind == SyntaxKind.MultisetKeyword)
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
        SyntaxToken? fromKeyword = null;
        SyntaxToken? fromPosition = null;
        if (_current.Kind == SyntaxKind.FromKeyword
            && (NextIs(Keyword.First) || NextIs(Keyword.Last)))
        {
            fromKeyword = Advance();
            fromPosition = Advance();
        }

        SyntaxToken? nullTreatment = null;
        SyntaxToken? nullsKeyword = null;
        if (IdentifierEquals(Keyword.Respect) || IdentifierEquals(Keyword.Ignore))
        {
            nullTreatment = Advance();
            if (!IdentifierEquals(Keyword.Nulls))
            {
                throw new SqlParseException($"Expected NULLS, found {_current.Kind}", _current.Position);
            }

            nullsKeyword = Advance();
        }

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
            NullTreatment = nullTreatment,
            NullsKeyword = nullsKeyword,
            Filter = filter,
            Over = over,
        };
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
