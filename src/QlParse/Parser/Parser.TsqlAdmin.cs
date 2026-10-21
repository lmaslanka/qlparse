namespace QlParse;

// Workstream G: SQL Server administration commands, Service Broker and full-text DDL. Every statement starts
// with a word ISO and Postgres lex as a plain identifier (or END for END CONVERSATION) and the hook only runs
// under SqlOptions.SqlServer, so a statement that isn't ours returns null after a length switch and a compare.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlAdminStatement() => _current.Kind switch
    {
        SyntaxKind.Identifier => TryParseTsqlAdminWord(_current.TextOf(_source)),
        SyntaxKind.EndKeyword => IsTsqlEndConversation() ? ParseTsqlEndConversation() : null,
        _ => null,
    };

    // END CONVERSATION is the only statement that starts with END; the statement list asks this too.
    internal bool IsTsqlEndConversation() =>
        _current.Kind == SyntaxKind.EndKeyword && NextEquals(Keyword.AdminConversation);

    private Query? TryParseTsqlAdminWord(ReadOnlySpan<char> text) => text.Length switch
    {
        Len3 => TryParseTsqlAdminLen3(text),
        Len4 => TryParseTsqlAdminLen4(text),
        Len5 => TryParseTsqlAdminLen5(text),
        Len6 => TryParseTsqlAdminLen6(text),
        Len7 => TryParseTsqlAdminLen7(text),
        Len8 => TryParseTsqlAdminLen8(text),
        Len9 => TextEquals(text, Keyword.AdminWriteText) ? ParseTsqlWriteText() : null,
        Len10 => TryParseTsqlAdminLen10(text),
        Len11 => TextEquals(text, Keyword.AdminReconfigure) ? ParseTsqlReconfigure() : null,
        _ => null,
    };

    private Query? TryParseTsqlAdminLen3(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.Get) && NextEquals(Keyword.AdminConversation) ? ParseTsqlGetConversationGroup() : null;

    private Query? TryParseTsqlAdminLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.AdminDbcc))
        {
            return ParseTsqlDbcc();
        }

        if (TextEquals(text, Keyword.AdminKill))
        {
            return ParseTsqlKill();
        }

        if (TextEquals(text, Keyword.AdminSend))
        {
            return NextKind == SyntaxKind.OnKeyword ? ParseTsqlSend() : null;
        }

        return TextEquals(text, Keyword.AdminMove) && NextEquals(Keyword.AdminConversation) ? ParseTsqlMoveConversation() : null;
    }

    private Query? TryParseTsqlAdminLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Begin))
        {
            return TryParseTsqlBeginBroker();
        }

        return TextEquals(text, Keyword.Alter) ? TryParseTsqlAdminAlter() : null;
    }

    private Query? TryParseTsqlAdminLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Create))
        {
            return TryParseTsqlAdminCreate();
        }

        return TextEquals(text, Keyword.AdminBackup) && IsTsqlBackupTarget() ? ParseTsqlBackup() : null;
    }

    private Query? TryParseTsqlAdminLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.AdminRestore))
        {
            return IsTsqlRestoreTarget() ? ParseTsqlRestore() : null;
        }

        if (TextEquals(text, Keyword.AdminSetUser))
        {
            return ParseTsqlSetUser();
        }

        return TextEquals(text, Keyword.AdminReceive) ? ParseTsqlReceive() : null;
    }

    private Query? TryParseTsqlAdminLen8(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.AdminReadText))
        {
            return ParseTsqlReadText();
        }

        return TextEquals(text, Keyword.AdminShutdown) ? ParseTsqlShutdown() : null;
    }

    private Query? TryParseTsqlAdminLen10(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.AdminCheckpoint))
        {
            return ParseTsqlCheckpoint();
        }

        return TextEquals(text, Keyword.AdminUpdateText) ? ParseTsqlUpdateText() : null;
    }

    // CREATE <object>: the object words decide; anything else is left to the other hooks and ISO.
    private Query? TryParseTsqlAdminCreate() =>
        TryParseTsqlBrokerCreate() ?? TryParseTsqlServerCreate() ?? TryParseTsqlFulltextCreate();

    private Query? TryParseTsqlAdminAlter() =>
        TryParseTsqlBrokerAlter() ?? TryParseTsqlServerAlter() ?? TryParseTsqlFulltextAlter();

    // `name` or `@variable`, as a database, device or handle.
    internal SyntaxToken ParseTsqlNameOrVariable()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected name or variable, found {_current.Kind}", _current.Position);
    }

    internal SyntaxToken ExpectTsqlVariable() => Expect(SyntaxKind.EmbeddedHost);

    // `AUTHORIZATION owner`, shared by the Service Broker and full-text CREATE statements.
    internal (SyntaxToken? Keyword, SyntaxToken? Owner) ParseTsqlAuthorization()
    {
        if (!IdentifierEquals(Keyword.Authorization))
        {
            return (null, null);
        }

        var keyword = Advance();
        return (keyword, Expect(SyntaxKind.Identifier));
    }

    // ---- Generic option lists ---------------------------------------------------------------------------
    // SQL Server's administrative statements share one option shape: `name [=] value`, a bare flag, or
    // `name [=] (nested, ...)`. Inside parentheses an item runs to the next `,` or `)`; a bare list (after WITH,
    // SET, ...) has no closing token, so its values stay short: one value, its unit, and `TO`/`AFTER` value.

    // `( item [, ...] )`; the list may be empty, as in `FOR TSQL ()`.
    internal TsqlOptionList ParseTsqlOptionList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var options = new List<TsqlOption>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            options.Add(ParseTsqlOption(bounded: true, allowConnector: true));
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                options.Add(ParseTsqlOption(bounded: true, allowConnector: true));
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlOptionList
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Options = options,
            CloseParen = closeParen,
        };
    }

    // `item [, ...]` without parentheses. allowConnector lets a value continue with `TO`/`AFTER` value
    // (`MOVE 'data' TO 'path'`, `CPU = 0 TO 3`).
    internal TsqlOptionList ParseTsqlBareOptionList(bool allowConnector)
    {
        var options = new List<TsqlOption> { ParseTsqlOption(bounded: false, allowConnector) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlOption(bounded: false, allowConnector));
        }

        return new TsqlOptionList { Span = SourceSpan.From(options[0].Span, options[^1].Span), Options = options };
    }

    // `WITH (item, ...)` or `WITH item, ...`.
    internal TsqlOptionList ParseTsqlWithOptions() =>
        _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : ParseTsqlBareOptionList(allowConnector: true);

    internal TsqlOption ParseTsqlOption(bool bounded, bool allowConnector)
    {
        var start = _current.Span;
        var name = ParseTsqlOptionName(bounded);
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var value = bounded
            ? ParseTsqlBoundedValue()
            : ParseTsqlBareValue(name.Count == 0 || equalsToken is not null, allowConnector);
        var options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : null;
        if (name.Count == 0 && equalsToken is null && value.Count == 0 && options is null)
        {
            throw new SqlParseException($"Expected option, found {_current.Kind}", _current.Position);
        }

        return new TsqlOption
        {
            Span = SourceSpan.From(start, TsqlOptionEnd(name, equalsToken, value, options)),
            Name = name,
            EqualsToken = equalsToken,
            Value = value,
            Options = options,
        };
    }

    private static SourceSpan TsqlOptionEnd(
        IReadOnlyList<SyntaxToken> name,
        SyntaxToken? equalsToken,
        IReadOnlyList<SyntaxToken> value,
        TsqlOptionList? options)
    {
        if (options is not null)
        {
            return options.Span;
        }

        if (value.Count > 0)
        {
            return value[^1].Span;
        }

        return equalsToken?.Span ?? name[^1].Span;
    }

    // Option words are identifiers in a bare list. Inside parentheses keywords count too, as in WITH IP,
    // GROUP or EXECUTE AS SELF, because the list's own closing parenthesis ends the item.
    private static bool IsTsqlOptionWord(SyntaxKind kind, bool bounded) =>
        kind == SyntaxKind.Identifier || (bounded && IsTsqlKeywordKind(kind) && kind != SyntaxKind.OnKeyword);

    // The keyword kinds form one contiguous block of SyntaxKind.
    private static bool IsTsqlKeywordKind(SyntaxKind kind) =>
        kind is >= SyntaxKind.SelectKeyword and <= SyntaxKind.WidthBucketKeyword;

    // ON / OFF start a value, never a name (`SET SOFTNUMA OFF`, `STATUS = ON`).
    private bool IsTsqlSwitchValue() => _current.Kind == SyntaxKind.OnKeyword || IdentifierEquals(Keyword.AdminOff);

    // A run of option words is the whole name when `=` or `(` follows it. Otherwise, inside parentheses, its
    // last word starts the value (`CHANGE_TRACKING OFF`, `EXECUTE AS SELF`); in a bare list only the first
    // word is taken, so the next statement's verb is never swallowed.
    private IReadOnlyList<SyntaxToken> ParseTsqlOptionName(bool bounded)
    {
        if (IsTsqlSwitchValue() || !IsTsqlOptionWord(_current.Kind, bounded))
        {
            return [];
        }

        var count = TsqlOptionNameLength(bounded);
        var name = new List<SyntaxToken>(count);
        for (var i = 0; i < count; i++)
        {
            name.Add(Advance());
        }

        return name;
    }

    private int TsqlOptionNameLength(bool bounded)
    {
        var index = _index;
        while (index < _tokens.Count && IsTsqlOptionWord(_tokens[index].Kind, bounded) && !IsTsqlOffAt(index))
        {
            index++;
        }

        var runLength = index - _index + 1;
        var after = index < _tokens.Count ? _tokens[index].Kind : SyntaxKind.EndOfFile;
        if (after is SyntaxKind.EqualsToken or SyntaxKind.OpenParen)
        {
            return runLength;
        }

        return bounded && runLength > 1 ? runLength - 1 : 1;
    }

    private bool IsTsqlOffAt(int index) => TokenEquals(_tokens[index], Keyword.AdminOff);

    // Inside parentheses a value is every token up to the item's `,` or `)` (or a nested list's `(`).
    private IReadOnlyList<SyntaxToken> ParseTsqlBoundedValue()
    {
        if (IsTsqlBoundedValueEnd())
        {
            return [];
        }

        var value = new List<SyntaxToken> { Advance() };
        while (!IsTsqlBoundedValueEnd())
        {
            value.Add(Advance());
        }

        return value;
    }

    private bool IsTsqlBoundedValueEnd() => _current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen
        or SyntaxKind.OpenParen or SyntaxKind.EndOfFile or SyntaxKind.Semicolon or SyntaxKind.BatchSeparator;

    // In a bare list a value is `[sign] token [unit]`, optionally followed by `TO`/`AFTER` and a second one.
    // A word (ON, AUTO, dbo.fn, ...) is only a value after `=`; without it only a literal or ON/OFF is.
    private IReadOnlyList<SyntaxToken> ParseTsqlBareValue(bool allowWord, bool allowConnector)
    {
        if (!IsTsqlLiteralStart() && !IsTsqlSwitchValue() && !(allowWord && IsTsqlValueWord()))
        {
            return [];
        }

        var value = new List<SyntaxToken>();
        AddTsqlValueAtom(value);
        if (allowConnector && IsTsqlValueConnector())
        {
            value.Add(Advance());
            AddTsqlValueAtom(value);
        }

        return value;
    }

    private bool IsTsqlValueConnector() =>
        (IdentifierEquals(Keyword.To) || IdentifierEquals(Keyword.After))
        && _index < _tokens.Count
        && _tokens[_index].Kind is SyntaxKind.String or SyntaxKind.Number or SyntaxKind.EmbeddedHost or SyntaxKind.MinusToken;

    private bool IsTsqlLiteralStart() => _current.Kind is SyntaxKind.String or SyntaxKind.Number
        or SyntaxKind.EmbeddedHost or SyntaxKind.BinaryLiteral or SyntaxKind.MoneyLiteral
        or SyntaxKind.MinusToken or SyntaxKind.PlusToken;

    private bool IsTsqlValueWord() => _current.Kind == SyntaxKind.Identifier || IsTsqlKeywordKind(_current.Kind);

    private void AddTsqlValueAtom(List<SyntaxToken> value)
    {
        if (_current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken)
        {
            value.Add(Advance());
        }

        if (!IsTsqlLiteralStart() && !IsTsqlValueWord())
        {
            throw new SqlParseException($"Expected option value, found {_current.Kind}", _current.Position);
        }

        var token = Advance();
        value.Add(token);
        if (token.Kind is SyntaxKind.Number or SyntaxKind.MoneyLiteral)
        {
            if (_current.Kind == SyntaxKind.Identifier && Keyword.IsTsqlAdminUnit(_current.TextOf(_source)))
            {
                value.Add(Advance());
            }

            return;
        }

        while (token.Kind == SyntaxKind.Identifier && _current.Kind == SyntaxKind.Dot && NextKind == SyntaxKind.Identifier)
        {
            value.Add(Advance());
            token = Advance();
            value.Add(token);
        }
    }
}
