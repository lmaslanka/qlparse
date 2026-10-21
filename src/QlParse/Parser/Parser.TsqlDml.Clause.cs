namespace QlParse;

// SQL Server clauses shared by SELECT and the DML statements (workstream C): TOP, OUTPUT, OPTION (...) query
// hints, WITH (...) table hints, FOR XML / FOR JSON / FOR BROWSE, and multi-part names.
internal sealed partial class Parser
{
    // TOP (expression) [PERCENT] [WITH TIES]; a SELECT also takes the legacy TOP n, DML only the parenthesized form.
    private TsqlTopClause ParseTsqlTop(bool isSelect)
    {
        var topKeyword = Advance();
        var (openParen, count, closeParen) = ParseTsqlTopCount(isSelect);
        var percentKeyword = IdentifierEquals(Keyword.Percent) ? Advance() : (SyntaxToken?)null;
        SyntaxToken? withKeyword = null;
        SyntaxToken? tiesKeyword = null;
        if (isSelect && _current.Kind == SyntaxKind.WithKeyword && NextEquals(Keyword.Ties))
        {
            withKeyword = Advance();
            tiesKeyword = Advance();
        }

        var end = tiesKeyword ?? percentKeyword ?? closeParen;
        return new TsqlTopClause
        {
            Span = end is null ? SourceSpan.From(topKeyword, count.Span) : SourceSpan.From(topKeyword, end.Value),
            TopKeyword = topKeyword,
            OpenParen = openParen,
            Count = count,
            CloseParen = closeParen,
            PercentKeyword = percentKeyword,
            WithKeyword = withKeyword,
            TiesKeyword = tiesKeyword,
        };
    }

    private (SyntaxToken? OpenParen, Expression Count, SyntaxToken? CloseParen) ParseTsqlTopCount(bool isSelect)
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            var count = ParseExpression();
            return (openParen, count, Expect(SyntaxKind.CloseParen));
        }

        return isSelect && _current.Kind == SyntaxKind.Number
            ? (null, Literal(), null)
            : throw new SqlParseException($"Expected ( after TOP, found {_current.Kind}", _current.Position);
    }

    private TsqlTopClause? ParseOptionalTsqlDmlTop() => IdentifierEquals(Keyword.DmlTop) ? ParseTsqlTop(isSelect: false) : null;

    // [server.][database.][schema.]object. An omitted part (`db..t`, `.schema.t`) is a zero-length identifier
    // token at the position of the following dot; a table variable (`@t`) is a one-part name.
    internal List<SyntaxToken> ParseTsqlMultiPartName()
    {
        var parts = new List<SyntaxToken> { ExpectTsqlNamePart() };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            parts.Add(ExpectTsqlNamePart());
        }

        return parts;
    }

    // A name part that is not an ordinary identifier: an omitted part before a dot, or a table variable. Without
    // SQL Server this fails exactly like Expect(Identifier).
    internal SyntaxToken ExpectTsqlNamePart()
    {
        if (IsSqlServer)
        {
            if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.EmbeddedHost)
            {
                return Advance();
            }

            if (_current.Kind == SyntaxKind.Dot)
            {
                return new SyntaxToken(SyntaxKind.Identifier, _current.Position, 0, _current.LeadingTriviaStart, 0);
            }
        }

        return Expect(SyntaxKind.Identifier);
    }

    // Hint names, FOR XML directives and option names: identifiers and keyword tokens alike.
    private bool IsTsqlDmlWord(SyntaxToken token) =>
        token.Kind == SyntaxKind.Identifier
        || token.Kind is not (SyntaxKind.String or SyntaxKind.Number) && token.Length > 0 && char.IsAsciiLetter(_source[token.Position]);

    private SyntaxToken ExpectTsqlDmlWord() =>
        IsTsqlDmlWord(_current) ? Advance() : throw new SqlParseException($"Expected name, found {_current.Kind}", _current.Position);

    // [[AS] alias]: a bare alias is never a T-SQL reserved word.
    private readonly record struct TsqlAlias(SyntaxToken? AsKeyword, SyntaxToken? Alias);

    private TsqlAlias ParseTsqlAlias()
    {
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            var asKeyword = Advance();
            return new TsqlAlias(asKeyword, Expect(SyntaxKind.Identifier));
        }

        return _current.Kind == SyntaxKind.Identifier && !IsTsqlReservedAliasWord() ? new TsqlAlias(null, Advance()) : default;
    }

    // ---- OUTPUT ----

    private IReadOnlyList<TsqlOutputClause> ParseTsqlOutputClauses()
    {
        if (!IdentifierEquals(Keyword.Output))
        {
            return [];
        }

        var clauses = new List<TsqlOutputClause>();
        while (IdentifierEquals(Keyword.Output))
        {
            clauses.Add(ParseTsqlOutputClause());
        }

        return clauses;
    }

    private TsqlOutputClause ParseTsqlOutputClause()
    {
        var outputKeyword = Advance();
        var columns = ParseTsqlSelectList();
        if (!IdentifierEquals(Keyword.Into))
        {
            return new TsqlOutputClause
            {
                Span = SourceSpan.From(outputKeyword, columns[^1].Span),
                OutputKeyword = outputKeyword,
                Columns = columns,
            };
        }

        var intoKeyword = Advance();
        var target = ParseTsqlMultiPartName();
        ParseOptionalColumnList(out var open, out var targetColumns, out var close);
        return new TsqlOutputClause
        {
            Span = SourceSpan.From(outputKeyword, close ?? target[^1]),
            OutputKeyword = outputKeyword,
            Columns = columns,
            IntoKeyword = intoKeyword,
            Target = target,
            TargetOpenParen = open,
            TargetColumns = targetColumns,
            TargetCloseParen = close,
        };
    }

    // ---- OPTION (query hints) and WITH (table hints) ----

    private TsqlOptionClause? ParseOptionalTsqlOption() =>
        IdentifierEquals(Keyword.DmlOption) && NextKind == SyntaxKind.OpenParen ? ParseTsqlOptionClause() : null;

    private TsqlOptionClause ParseTsqlOptionClause()
    {
        var optionKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var hints = new List<TsqlHint> { ParseTsqlQueryHint() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            hints.Add(ParseTsqlQueryHint());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlOptionClause
        {
            Span = SourceSpan.From(optionKeyword, closeParen),
            OptionKeyword = optionKeyword,
            OpenParen = openParen,
            Hints = hints,
            CloseParen = closeParen,
        };
    }

    // Query hints are word sequences (`HASH GROUP`, `OPTIMIZE FOR UNKNOWN`, `USE HINT (...)`), comma-separated.
    private TsqlHint ParseTsqlQueryHint()
    {
        var words = new List<SyntaxToken> { ExpectTsqlDmlWord() };
        while (IsTsqlDmlWord(_current))
        {
            words.Add(Advance());
        }

        return FinishTsqlHint(words, bareValue: true);
    }

    // `table WITH (hint [[,] hint] ...)`: each table hint is one word; commas between hints are optional. The
    // legacy form drops the WITH (see IsTsqlLegacyTableHints).
    internal TsqlHintedTable ParseTsqlHintedTable(TableSource table)
    {
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var openParen = Expect(SyntaxKind.OpenParen);
        var hints = new List<TsqlHint>();
        do
        {
            hints.Add(FinishTsqlHint([ExpectTsqlDmlWord()], bareValue: false));
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
            }
        }
        while (_current.Kind != SyntaxKind.CloseParen);

        var closeParen = Advance();
        return new TsqlHintedTable
        {
            Span = SourceSpan.From(table.Span, closeParen),
            Table = table,
            WithKeyword = withKeyword,
            OpenParen = openParen,
            Hints = hints,
            CloseParen = closeParen,
        };
    }

    private TsqlHint FinishTsqlHint(List<SyntaxToken> words, bool bareValue)
    {
        var start = words[0];
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var equalsToken = Advance();
            var value = ParseExpression();
            return new TsqlHint { Span = SourceSpan.From(start, value.Span), Keywords = words, EqualsToken = equalsToken, Value = value };
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            var arguments = new List<TsqlHintArgument> { ParseTsqlHintArgument() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParseTsqlHintArgument());
            }

            var closeParen = Expect(SyntaxKind.CloseParen);
            return new TsqlHint
            {
                Span = SourceSpan.From(start, closeParen),
                Keywords = words,
                OpenParen = openParen,
                Arguments = arguments,
                CloseParen = closeParen,
            };
        }

        if (bareValue && _current.Kind is SyntaxKind.Number or SyntaxKind.String or SyntaxKind.EmbeddedHost)
        {
            var value = ParseExpression();
            return new TsqlHint { Span = SourceSpan.From(start, value.Span), Keywords = words, Value = value };
        }

        return new TsqlHint { Span = SourceSpan.From(start, words[^1]), Keywords = words };
    }

    // `ix`, `'NAME'`, `@p = 1`, `@p UNKNOWN`, `col DESC`.
    private TsqlHintArgument ParseTsqlHintArgument()
    {
        var value = ParseExpression(ComparisonBindingPower + 1);
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var equalsToken = Advance();
            var assigned = ParseExpression();
            return new TsqlHintArgument { Span = SourceSpan.From(value.Span, assigned.Span), Value = value, EqualsToken = equalsToken, Assigned = assigned };
        }

        if (IsTsqlDmlWord(_current))
        {
            var modifier = Advance();
            return new TsqlHintArgument { Span = SourceSpan.From(value.Span, modifier), Value = value, Modifier = modifier };
        }

        return new TsqlHintArgument { Span = value.Span, Value = value };
    }

    // ---- FOR XML / FOR JSON / FOR BROWSE ----

    private bool IsTsqlForClause() =>
        _current.Kind == SyntaxKind.ForKeyword
        && (NextEquals(Keyword.Xml) || NextEquals(Keyword.Json) || NextEquals(Keyword.DmlBrowse));

    private TsqlForClause ParseTsqlForClause()
    {
        var forKeyword = Advance();
        var format = Advance();
        if (TokenEquals(format, Keyword.DmlBrowse))
        {
            return new TsqlForClause { Span = SourceSpan.From(forKeyword, format), ForKeyword = forKeyword, Format = format };
        }

        var options = new List<TsqlForOption> { ParseTsqlForOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlForOption());
        }

        return new TsqlForClause
        {
            Span = SourceSpan.From(forKeyword, options[^1].Span),
            ForKeyword = forKeyword,
            Format = format,
            Options = options,
        };
    }

    // ELEMENTS {XSINIL | ABSENT}, BINARY BASE64
    private bool IsTsqlForOptionSecondWord(SyntaxToken first) =>
        TokenEquals(first, Keyword.DmlElements)
            ? IdentifierEquals(Keyword.DmlXsinil) || IdentifierEquals(Keyword.Absent)
            : TokenEquals(first, Keyword.DmlBinary) && IdentifierEquals(Keyword.DmlBase64);

    // RAW | AUTO | EXPLICIT | PATH | ROOT | TYPE | XMLSCHEMA ... [('argument')], ELEMENTS [XSINIL | ABSENT], BINARY BASE64.
    private TsqlForOption ParseTsqlForOption()
    {
        var first = ExpectTsqlDmlWord();
        List<SyntaxToken> keywords = [first];
        if (IsTsqlForOptionSecondWord(first))
        {
            keywords.Add(Advance());
        }

        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlForOption { Span = SourceSpan.From(first, keywords[^1]), Keywords = keywords };
        }

        var openParen = Advance();
        var argument = _current.Kind == SyntaxKind.CloseParen ? null : ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlForOption
        {
            Span = SourceSpan.From(first, closeParen),
            Keywords = keywords,
            OpenParen = openParen,
            Argument = argument,
            CloseParen = closeParen,
        };
    }
}
