namespace QlParse;

internal sealed partial class Parser
{
    // COPY { table [(columns)] | (query) } {FROM|TO} {'file'|PROGRAM 'cmd'|STDIN|STDOUT}
    //     [[WITH] (option [, ...]) | legacy options] [WHERE condition]
    // Inline data rows after FROM STDIN belong to the psql client protocol and are not parsed.
    private CopyStatement ParseCopy()
    {
        var copyKeyword = Advance();
        var source = ParseCopySource();
        var direction = ParseCopyDirection();
        var program = IdentifierEquals(Keyword.Program) ? Advance() : (SyntaxToken?)null;
        var target = ParseCopyTarget();
        var options = ParseCopyOptions();
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        var end = where?.Span ?? options.End ?? target.Span;
        return new CopyStatement
        {
            Span = SourceSpan.From(copyKeyword, end),
            CopyKeyword = copyKeyword,
            TableName = source.TableName,
            ColumnOpenParen = source.ColumnOpenParen,
            Columns = source.Columns,
            ColumnCloseParen = source.ColumnCloseParen,
            QueryOpenParen = source.QueryOpenParen,
            Query = source.Query,
            QueryCloseParen = source.QueryCloseParen,
            DirectionKeyword = direction,
            ProgramKeyword = program,
            Target = target,
            WithKeyword = options.WithKeyword,
            OptionsOpenParen = options.OpenParen,
            Options = options.Options,
            OptionsCloseParen = options.CloseParen,
            Where = where,
        };
    }

    private readonly record struct CopySource(
        IReadOnlyList<SyntaxToken> TableName,
        SyntaxToken? ColumnOpenParen,
        IReadOnlyList<SyntaxToken>? Columns,
        SyntaxToken? ColumnCloseParen,
        SyntaxToken? QueryOpenParen,
        Query? Query,
        SyntaxToken? QueryCloseParen);

    private CopySource ParseCopySource()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var queryOpen = Advance();
            var query = ParsePgQueryOrDml(0);
            var queryClose = Expect(SyntaxKind.CloseParen);
            return new CopySource([], null, null, null, queryOpen, query, queryClose);
        }

        var tableName = ParseQualifiedName();
        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        return new CopySource(tableName, columnOpen, columns, columnClose, null, null, null);
    }

    private SyntaxToken ParseCopyDirection()
    {
        if (_current.Kind == SyntaxKind.FromKeyword || IdentifierEquals(Keyword.To))
        {
            return Advance();
        }

        throw new SqlParseException($"Expected FROM or TO, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ParseCopyTarget()
    {
        if (_current.Kind == SyntaxKind.String || IdentifierEquals(Keyword.Stdin) || IdentifierEquals(Keyword.Stdout))
        {
            return Advance();
        }

        throw new SqlParseException($"Expected file name, STDIN, or STDOUT, found {_current.Kind}", _current.Position);
    }

    private readonly record struct CopyOptions(
        SyntaxToken? WithKeyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<CopyOption> Options,
        SyntaxToken? CloseParen,
        SourceSpan? End);

    private CopyOptions ParseCopyOptions()
    {
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            var options = new List<CopyOption> { ParseCopyGenericOption() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                options.Add(ParseCopyGenericOption());
            }

            var closeParen = Expect(SyntaxKind.CloseParen);
            return new CopyOptions(withKeyword, openParen, options, closeParen, closeParen.Span);
        }

        var legacy = ParseLegacyCopyOptions();
        if (withKeyword is not null && legacy.Count == 0)
        {
            throw new SqlParseException($"Expected COPY option, found {_current.Kind}", _current.Position);
        }

        var end = legacy.Count > 0 ? legacy[^1].Span : withKeyword?.Span;
        return new CopyOptions(withKeyword, null, legacy, null, end);
    }

    private CopyOption ParseCopyGenericOption()
    {
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.NullKeyword or SyntaxKind.EscapeKeyword))
        {
            throw new SqlParseException($"Expected COPY option name, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        if (_current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen)
        {
            return new CopyOption { Span = name.Span, Name = name };
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            var values = ParseNameList();
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new CopyOption
            {
                Span = SourceSpan.From(name, closeParen),
                Name = name,
                OpenParen = openParen,
                Values = values,
                CloseParen = closeParen,
            };
        }

        var value = ParseCopyOptionValue();
        return new CopyOption { Span = SourceSpan.From(name, value), Name = name, Value = value };
    }

    private SyntaxToken ParseCopyOptionValue()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.Number or SyntaxKind.Star
            or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.OnKeyword or SyntaxKind.MatchKeyword)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected COPY option value, found {_current.Kind}", _current.Position);
    }

    private IReadOnlyList<CopyOption> ParseLegacyCopyOptions()
    {
        if (!IsLegacyCopyOptionStart())
        {
            return [];
        }

        var options = new List<CopyOption>();
        while (IsLegacyCopyOptionStart())
        {
            options.Add(ParseLegacyCopyOption());
        }

        return options;
    }

    private bool IsLegacyCopyOptionStart() =>
        IsLegacyCopyValueOption() || IsLegacyCopyFlag() || IdentifierEquals(Keyword.Force);

    private bool IsLegacyCopyFlag() =>
        IdentifierEquals(Keyword.Binary)
        || IdentifierEquals(Keyword.Csv)
        || IdentifierEquals(Keyword.Header)
        || IdentifierEquals(Keyword.Freeze)
        || IdentifierEquals(Keyword.Oids);

    private bool IsLegacyCopyValueOption() =>
        _current.Kind is SyntaxKind.NullKeyword or SyntaxKind.EscapeKeyword
        || IdentifierEquals(Keyword.Delimiter)
        || IdentifierEquals(Keyword.Quote)
        || IdentifierEquals(Keyword.Encoding);

    private CopyOption ParseLegacyCopyOption()
    {
        if (IdentifierEquals(Keyword.Force))
        {
            return ParseLegacyForceOption();
        }

        var name = Advance();
        if (!IsLegacyValueName(name))
        {
            return new CopyOption { Span = name.Span, Name = name };
        }

        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var value = Expect(SyntaxKind.String);
        return new CopyOption
        {
            Span = SourceSpan.From(name, value),
            Name = name,
            AsKeyword = asKeyword,
            Value = value,
        };
    }

    private bool IsLegacyValueName(SyntaxToken name) =>
        name.Kind is SyntaxKind.NullKeyword or SyntaxKind.EscapeKeyword
        || TokenEquals(name, Keyword.Delimiter)
        || TokenEquals(name, Keyword.Quote)
        || TokenEquals(name, Keyword.Encoding);

    // FORCE QUOTE {column [, ...] | *} | FORCE NOT NULL column [, ...] | FORCE NULL column [, ...]
    private CopyOption ParseLegacyForceOption()
    {
        var forceKeyword = Advance();
        IReadOnlyList<SyntaxToken> nameTail;
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            nameTail = [notKeyword, Expect(SyntaxKind.NullKeyword)];
        }
        else
        {
            if (_current.Kind != SyntaxKind.NullKeyword && !IdentifierEquals(Keyword.Quote))
            {
                throw new SqlParseException($"Expected QUOTE, NOT NULL, or NULL, found {_current.Kind}", _current.Position);
            }

            nameTail = [Advance()];
        }

        if (_current.Kind == SyntaxKind.Star)
        {
            var star = Advance();
            return new CopyOption { Span = SourceSpan.From(forceKeyword, star), Name = forceKeyword, NameTail = nameTail, Value = star };
        }

        var columns = ParseNameList();
        return new CopyOption
        {
            Span = SourceSpan.From(forceKeyword, columns[^1]),
            Name = forceKeyword,
            NameTail = nameTail,
            Values = columns,
        };
    }
}
