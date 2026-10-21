namespace QlParse;

// Shapes shared by the Postgres schema-object statements: the `(name = value, ...)` definition list,
// the foreign-data `OPTIONS ([ADD|SET|DROP] name 'value', ...)` list, operator names, routine argument
// type lists, IF [NOT] EXISTS, and role specifications.
internal sealed partial class Parser
{
    internal readonly record struct PgIfExists(SyntaxToken? IfKeyword, SyntaxToken? NotKeyword, SyntaxToken? ExistsKeyword);

    private readonly record struct PgDefinitionValue(
        SourceSpan End,
        SyntaxToken? Sign,
        SyntaxToken? Literal,
        DataType? Type,
        PgOperatorName? Operator);

    internal PgIfExists ParsePgIfNotExists()
    {
        if (!IdentifierEquals(Keyword.If) || NextKind != SyntaxKind.NotKeyword)
        {
            return default;
        }

        var ifKeyword = Advance();
        var notKeyword = Advance();
        return new PgIfExists(ifKeyword, notKeyword, Expect(SyntaxKind.ExistsKeyword));
    }

    internal PgIfExists ParsePgIfExists()
    {
        if (!IdentifierEquals(Keyword.If) || NextKind != SyntaxKind.ExistsKeyword)
        {
            return default;
        }

        var ifKeyword = Advance();
        return new PgIfExists(ifKeyword, null, Advance());
    }

    // Postgres accepts any keyword as a definition / option name (`JOIN = f`, `user 'x'`, `END = f`).
    private static bool IsPgWordKind(SyntaxKind kind) =>
        kind == SyntaxKind.Identifier || kind is >= SyntaxKind.SelectKeyword and <= SyntaxKind.WidthBucketKeyword;

    internal SyntaxToken ExpectPgWord()
    {
        if (!IsPgWordKind(_current.Kind))
        {
            throw new SqlParseException($"Expected name, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    internal SyntaxToken ExpectPgNameOrString()
    {
        if (_current.Kind is not SyntaxKind.Identifier and not SyntaxKind.String)
        {
            throw new SqlParseException($"Expected name or string, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // OWNER TO / FOR user: a role name or one of the niladic role keywords.
    internal SyntaxToken ExpectPgRole()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.UserKeyword or SyntaxKind.CurrentUserKeyword
            or SyntaxKind.SessionUserKeyword or SyntaxKind.CurrentRoleKeyword)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected role, found {_current.Kind}", _current.Position);
    }

    internal SyntaxToken? ParsePgOptionalBehavior() =>
        IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict) ? Advance() : null;

    internal bool IsPgStatementEnd() => _current.Kind is SyntaxKind.EndOfFile or SyntaxKind.Semicolon;

    internal PgDefinitionList ParsePgDefinitionList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = new List<PgDefinitionElement>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            elements.Add(ParsePgDefinitionElement());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                elements.Add(ParsePgDefinitionElement());
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgDefinitionList
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
        };
    }

    private PgDefinitionElement ParsePgDefinitionElement()
    {
        var name = ExpectPgWord();
        SyntaxToken? equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : null;
        if (equalsToken is null && _current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen)
        {
            return new PgDefinitionElement { Span = name.Span, Name = name };
        }

        var value = ParsePgDefinitionValue();
        return new PgDefinitionElement
        {
            Span = SourceSpan.From(name, value.End),
            Name = name,
            EqualsToken = equalsToken,
            Sign = value.Sign,
            Literal = value.Literal,
            Type = value.Type,
            Operator = value.Operator,
        };
    }

    private PgDefinitionValue ParsePgDefinitionValue()
    {
        if (IsQualifiedOperatorStart() || (IsOperatorKind(_current.Kind) && NextKind != SyntaxKind.Number))
        {
            var operatorName = ParsePgOperatorName();
            return new PgDefinitionValue(operatorName.Span, null, null, null, operatorName);
        }

        if (_current.Kind is SyntaxKind.PlusToken or SyntaxKind.MinusToken)
        {
            var sign = Advance();
            var number = Expect(SyntaxKind.Number);
            return new PgDefinitionValue(number.Span, sign, number, null, null);
        }

        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.DateKeyword or SyntaxKind.TimeKeyword
            or SyntaxKind.TimestampKeyword or SyntaxKind.IntervalKeyword)
        {
            var type = ParseDataType();
            return new PgDefinitionValue(type.Span, null, null, type, null);
        }

        if (_current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen or SyntaxKind.OpenParen or SyntaxKind.EndOfFile)
        {
            throw new SqlParseException($"Expected definition value, found {_current.Kind}", _current.Position);
        }

        var literal = Advance();
        return new PgDefinitionValue(literal.Span, null, literal, null, null);
    }

    internal bool IsPgOptionsStart() => IdentifierEquals(Keyword.Options) && NextKind == SyntaxKind.OpenParen;

    internal PgOptionList? ParsePgOptionalOptionList() => IsPgOptionsStart() ? ParsePgOptionList() : null;

    internal PgOptionList ParsePgOptionList()
    {
        var optionsKeyword = ExpectIdent(Keyword.Options);
        var openParen = Expect(SyntaxKind.OpenParen);
        var items = new List<PgOptionItem> { ParsePgOptionItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParsePgOptionItem());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgOptionList
        {
            Span = SourceSpan.From(optionsKeyword, closeParen),
            OptionsKeyword = optionsKeyword,
            OpenParen = openParen,
            Items = items,
            CloseParen = closeParen,
        };
    }

    private PgOptionItem ParsePgOptionItem()
    {
        var start = _current;
        SyntaxToken? action = null;
        if ((IdentifierEquals(Keyword.Add) || _current.Kind == SyntaxKind.SetKeyword || IdentifierEquals(Keyword.Drop))
            && IsPgWordKind(NextKind))
        {
            action = Advance();
        }

        var name = ExpectPgWord();
        SyntaxToken? value = _current.Kind == SyntaxKind.String ? Advance() : null;
        return new PgOptionItem
        {
            Span = SourceSpan.From(start, value ?? name),
            Action = action,
            Name = name,
            Value = value,
        };
    }

    internal bool IsPgOperatorNameStart() =>
        IsOperatorKind(_current.Kind) || IsQualifiedOperatorStart()
        || (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.Dot);

    // `+`, `schema.===`, or `OPERATOR(schema.+)`.
    internal PgOperatorName ParsePgOperatorName()
    {
        var start = _current;
        SyntaxToken? operatorKeyword = null;
        SyntaxToken? openParen = null;
        if (IsQualifiedOperatorStart())
        {
            operatorKeyword = Advance();
            openParen = Advance();
        }

        var schemaParts = new List<SyntaxToken>();
        while (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.Dot)
        {
            schemaParts.Add(Advance());
            Advance();
        }

        if (!IsOperatorKind(_current.Kind))
        {
            throw new SqlParseException($"Expected operator, found {_current.Kind}", _current.Position);
        }

        var operatorToken = Advance();
        SyntaxToken? closeParen = openParen is null ? null : Expect(SyntaxKind.CloseParen);
        return new PgOperatorName
        {
            Span = SourceSpan.From(start, closeParen ?? operatorToken),
            OperatorKeyword = operatorKeyword,
            OpenParen = openParen,
            SchemaParts = schemaParts,
            Operator = operatorToken,
            CloseParen = closeParen,
        };
    }

    internal List<PgRoutineArgument> ParsePgRoutineArguments()
    {
        var arguments = new List<PgRoutineArgument> { ParsePgRoutineArgument() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParsePgRoutineArgument());
        }

        return arguments;
    }

    private PgRoutineArgument ParsePgRoutineArgument()
    {
        var start = _current;
        SyntaxToken? mode = null;
        if (IsPgArgumentMode() && NextKind is not SyntaxKind.Comma and not SyntaxKind.CloseParen)
        {
            mode = Advance();
        }

        var type = ParseDataType();
        return new PgRoutineArgument { Span = SourceSpan.From(start, type.Span), Mode = mode, Type = type };
    }

    private bool IsPgArgumentMode() =>
        _current.Kind == SyntaxKind.InKeyword || IdentifierEquals(Keyword.Out)
        || IdentifierEquals(Keyword.Inout) || IdentifierEquals(Keyword.Variadic);

    internal List<SyntaxToken> ParsePgObjectKindTokens(int count)
    {
        var tokens = new List<SyntaxToken>(count);
        for (var i = 0; i < count; i++)
        {
            tokens.Add(Advance());
        }

        return tokens;
    }

    internal List<SyntaxToken> ParsePgStringList()
    {
        var values = new List<SyntaxToken> { Expect(SyntaxKind.String) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            values.Add(Expect(SyntaxKind.String));
        }

        return values;
    }
}
