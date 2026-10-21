namespace QlParse;

// Workstream D: SQL Server tables, partitioning, indexes, views, synonyms, types, graph and constraints.
// Claims the CREATE, ALTER and DROP forms whose T-SQL grammar differs from ISO, plus UPDATE STATISTICS. A
// statement with an ISO counterpart keeps the ISO node and carries its T-SQL-only parts in a SqlServer
// extras holder.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlDdlStatement()
    {
        if (_index >= _tokens.Count)
        {
            return null;
        }

        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            return NextEquals(Keyword.DdlStatistics) ? ParseTsqlUpdateStatistics() : null;
        }

        if (_current.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        var text = _current.TextOf(_source);
        return text.Length switch
        {
            Len4 => TextEquals(text, Keyword.Drop) ? TryParseTsqlDdlDrop() : null,
            Len5 => TextEquals(text, Keyword.Alter) ? TryParseTsqlDdlAlter() : null,
            Len6 => TextEquals(text, Keyword.Create) ? TryParseTsqlDdlCreate() : null,
            _ => null,
        };
    }

    private Query? TryParseTsqlDdlCreate()
    {
        var next = _tokens[_index];
        if (next.Kind == SyntaxKind.UniqueKeyword)
        {
            return ParseTsqlCreateIndex();
        }

        if (next.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        var text = next.TextOf(_source);
        return TryParseTsqlCreateTableOrView(text) ?? TryParseTsqlCreateIndexKind(text) ?? TryParseTsqlCreateObject(text);
    }

    private Query? TryParseTsqlCreateTableOrView(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Table))
        {
            return ParseTsqlCreateTable();
        }

        if (TextEquals(text, Keyword.View))
        {
            return ParseTsqlCreateView();
        }

        return TextEquals(text, Keyword.Type) ? TryParseTsqlCreateType() : null;
    }

    private Query? TryParseTsqlCreateIndexKind(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Index))
        {
            return ParseTsqlCreateIndex();
        }

        if (TextEquals(text, Keyword.Clustered) || TextEquals(text, Keyword.Nonclustered))
        {
            return IsKeywordAt(_index + 1, Keyword.TsqlDdlColumnstore) ? ParseTsqlCreateSpecialIndex() : ParseTsqlCreateIndex();
        }

        return TryParseTsqlCreateSpecialIndexKind(text);
    }

    private Query? TryParseTsqlCreateSpecialIndexKind(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Primary) || TextEquals(text, Keyword.TsqlDdlColumnstore) || TextEquals(text, Keyword.TsqlDdlSpatial))
        {
            return ParseTsqlCreateSpecialIndex();
        }

        if (TextEquals(text, Keyword.Xml) || TextEquals(text, Keyword.Json) || TextEquals(text, Keyword.TsqlDdlVector))
        {
            return IsKeywordAt(_index + 1, Keyword.Index) ? ParseTsqlCreateSpecialIndex() : null;
        }

        return null;
    }

    private Query? TryParseTsqlCreateObject(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.TsqlDdlSynonym))
        {
            return ParseTsqlCreateSynonym();
        }

        if (TextEquals(text, Keyword.DdlStatistics))
        {
            return ParseTsqlCreateStatistics();
        }

        if (!TextEquals(text, Keyword.Partition))
        {
            return null;
        }

        if (IsKeywordAt(_index + 1, Keyword.Function))
        {
            return ParseTsqlCreatePartitionFunction();
        }

        return IsKeywordAt(_index + 1, Keyword.TsqlDdlScheme) ? ParseTsqlCreatePartitionScheme() : null;
    }

    private Query? TryParseTsqlDdlAlter()
    {
        if (NextEquals(Keyword.Table))
        {
            return ParseTsqlAlterTable();
        }

        if (NextEquals(Keyword.Index))
        {
            return ParseTsqlAlterIndex();
        }

        if (!NextEquals(Keyword.Partition))
        {
            return null;
        }

        if (IsKeywordAt(_index + 1, Keyword.Function))
        {
            return ParseTsqlAlterPartitionFunction();
        }

        return IsKeywordAt(_index + 1, Keyword.TsqlDdlScheme) ? ParseTsqlAlterPartitionScheme() : null;
    }

    private Query? TryParseTsqlDdlDrop()
    {
        if (NextEquals(Keyword.Index))
        {
            return ParseTsqlDropIndex();
        }

        var kindLength = TsqlDropObjectKindLength();
        return kindLength == 0 ? null : ParseTsqlDropObjects(kindLength);
    }

    // The number of tokens naming the object kind after DROP, or 0 when another parser owns it.
    private int TsqlDropObjectKindLength()
    {
        if (NextEquals(Keyword.Partition))
        {
            return IsKeywordAt(_index + 1, Keyword.Function) || IsKeywordAt(_index + 1, Keyword.TsqlDdlScheme) ? Len2 : 0;
        }

        var next = _tokens[_index];
        if (next.Kind != SyntaxKind.Identifier)
        {
            return 0;
        }

        var text = next.TextOf(_source);
        return IsTsqlDropTableKind(text) || IsTsqlDropModuleKind(text) ? 1 : 0;
    }

    private static bool IsTsqlDropTableKind(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.Table)
        || TextEquals(text, Keyword.View)
        || TextEquals(text, Keyword.Sequence)
        || TextEquals(text, Keyword.TsqlDdlSynonym)
        || TextEquals(text, Keyword.Type)
        || TextEquals(text, Keyword.DdlStatistics);

    private static bool IsTsqlDropModuleKind(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.Procedure)
        || TextEquals(text, Keyword.TsqlDdlProc)
        || TextEquals(text, Keyword.Function)
        || TextEquals(text, Keyword.Trigger);

    // ---- Shared clause parsers ----

    private static bool IsTsqlWordKind(SyntaxKind kind) =>
        kind == SyntaxKind.Identifier || kind is >= SyntaxKind.SelectKeyword and <= SyntaxKind.WidthBucketKeyword;

    // `WITH (option, ...)` or the unparenthesized legacy / statistics form; the WITH is already consumed.
    private TsqlRelationalOptionList ParseTsqlRelationalWithOptions() =>
        _current.Kind == SyntaxKind.OpenParen ? ParseTsqlRelationalOptions() : ParseTsqlBareRelationalOptions();

    internal TsqlRelationalOptionList ParseTsqlRelationalOptions()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var options = new List<TsqlRelationalOption> { ParseTsqlRelationalOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlRelationalOption());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlRelationalOptionList
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Options = options,
            CloseParen = closeParen,
        };
    }

    // `name [=] value-tokens [(nested)] [ON PARTITIONS (...)]` inside parentheses: the value is the run of
    // tokens up to the next `,`, `(` or `)`.
    private TsqlRelationalOption ParseTsqlRelationalOption()
    {
        if (IsTsqlRelationalOptionEnd())
        {
            throw new SqlParseException($"Expected option, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        List<SyntaxToken>? value = null;
        while (!IsTsqlRelationalOptionEnd())
        {
            value ??= [];
            value.Add(Advance());
        }

        var options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlRelationalOptions() : null;
        var partitions = TryParseTsqlOnPartitions();
        var end = partitions?.Span ?? options?.Span ?? (value is not null ? value[^1].Span : equalsToken?.Span ?? name.Span);
        return new TsqlRelationalOption
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            EqualsToken = equalsToken,
            Value = value ?? (IReadOnlyList<SyntaxToken>)[],
            Options = options,
            Partitions = partitions,
        };
    }

    private bool IsTsqlRelationalOptionEnd() =>
        _current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen or SyntaxKind.OpenParen or SyntaxKind.EndOfFile
            or SyntaxKind.Semicolon
        || (_current.Kind == SyntaxKind.OnKeyword && NextEquals(Keyword.TsqlDdlPartitions));

    private TsqlRelationalOptionList ParseTsqlBareRelationalOptions()
    {
        var options = new List<TsqlRelationalOption> { ParseTsqlBareRelationalOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlBareRelationalOption());
        }

        return new TsqlRelationalOptionList
        {
            Span = SourceSpan.From(options[0].Span, options[^1].Span),
            Options = options,
        };
    }

    // Unparenthesized option: `name [= value]` or `name number [PERCENT|ROWS]` (`SAMPLE 50 PERCENT`).
    private TsqlRelationalOption ParseTsqlBareRelationalOption()
    {
        var name = ExpectTsqlWord();
        SyntaxToken? equalsToken = null;
        List<SyntaxToken>? value = null;
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            equalsToken = Advance();
            value = _current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken ? [Advance()] : [];
            value.Add(IsTsqlWordKind(_current.Kind) ? Advance() : ExpectTsqlLiteralToken());
        }
        else
        {
            if (_current.Kind == SyntaxKind.Number)
            {
                value = [Advance()];
                if (IdentifierEquals(Keyword.Percent) || IdentifierEquals(Keyword.Rows))
                {
                    value.Add(Advance());
                }
            }
        }

        var partitions = TryParseTsqlOnPartitions();
        var end = partitions?.Span ?? (value is not null ? value[^1].Span : name.Span);
        return new TsqlRelationalOption
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            EqualsToken = equalsToken,
            Value = value ?? (IReadOnlyList<SyntaxToken>)[],
            Partitions = partitions,
        };
    }

    private SyntaxToken ExpectTsqlLiteralToken()
    {
        if (_current.Kind is not (SyntaxKind.Number or SyntaxKind.String or SyntaxKind.BinaryLiteral))
        {
            throw new SqlParseException($"Expected option value, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private TsqlOnPartitionsClause? TryParseTsqlOnPartitions()
    {
        if (_current.Kind != SyntaxKind.OnKeyword || !NextEquals(Keyword.TsqlDdlPartitions))
        {
            return null;
        }

        var onKeyword = Advance();
        var partitionsKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var ranges = new List<TsqlPartitionNumberRange> { ParseTsqlPartitionNumberRange() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            ranges.Add(ParseTsqlPartitionNumberRange());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlOnPartitionsClause
        {
            Span = SourceSpan.From(onKeyword, closeParen),
            OnKeyword = onKeyword,
            PartitionsKeyword = partitionsKeyword,
            OpenParen = openParen,
            Ranges = ranges,
            CloseParen = closeParen,
        };
    }

    private TsqlPartitionNumberRange ParseTsqlPartitionNumberRange()
    {
        var start = Expect(SyntaxKind.Number);
        if (!IdentifierEquals(Keyword.To))
        {
            return new TsqlPartitionNumberRange { Span = start.Span, Start = start };
        }

        var toKeyword = Advance();
        var end = Expect(SyntaxKind.Number);
        return new TsqlPartitionNumberRange
        {
            Span = SourceSpan.From(start, end),
            Start = start,
            ToKeyword = toKeyword,
            End = end,
        };
    }

    // `ON target [(column)]`, `TEXTIMAGE_ON target`, `FILESTREAM_ON target [(column)]`; the keyword is current.
    private TsqlStorageClause ParseTsqlStorageClause()
    {
        var keyword = Advance();
        var target = Expect(SyntaxKind.Identifier);
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlStorageClause { Span = SourceSpan.From(keyword, target), Keyword = keyword, Target = target };
        }

        var openParen = Advance();
        var column = Expect(SyntaxKind.Identifier);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlStorageClause
        {
            Span = SourceSpan.From(keyword, closeParen),
            Keyword = keyword,
            Target = target,
            OpenParen = openParen,
            Column = column,
            CloseParen = closeParen,
        };
    }

    private TsqlStorageClause? TryParseTsqlOnStorage() =>
        _current.Kind == SyntaxKind.OnKeyword ? ParseTsqlStorageClause() : null;

    private TsqlStorageClause? TryParseTsqlFilestreamOn() =>
        IdentifierEquals(Keyword.TsqlDdlFilestreamOn) ? ParseTsqlStorageClause() : null;

    private TsqlIncludeClause? TryParseTsqlInclude()
    {
        if (!IdentifierEquals(Keyword.DdlInclude))
        {
            return null;
        }

        var includeKeyword = Advance();
        ParseRequiredNameList(out var openParen, out var columns, out var closeParen);
        return new TsqlIncludeClause
        {
            Span = SourceSpan.From(includeKeyword, closeParen),
            IncludeKeyword = includeKeyword,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    // `[WITH (options)]` after an index-like definition: returns the WITH keyword and the list.
    private (SyntaxToken? WithKeyword, TsqlRelationalOptionList? Options) TryParseTsqlWithOptionsClause()
    {
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return (null, null);
        }

        var withKeyword = Advance();
        return (withKeyword, ParseTsqlRelationalWithOptions());
    }

    private ExistenceCheck? TryParseTsqlIfExists() => TryParsePgExistenceCheck();
}
