namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct UtilityOptionList(
        SyntaxToken? OpenParen,
        IReadOnlyList<KeywordOption> Options,
        SyntaxToken? CloseParen);

    // EXPLAIN [(option [value], ...)] statement | EXPLAIN [ANALYZE] [VERBOSE] statement
    internal ExplainStatement ParseExplain()
    {
        var explainKeyword = Advance();
        var options = IsUtilityOptionListStart()
            ? ParseUtilityOptionList()
            : ParseLegacyUtilityOptions(isVacuum: false);
        var statement = ParseStatement();
        return new ExplainStatement
        {
            Span = SourceSpan.From(explainKeyword, statement.Span),
            ExplainKeyword = explainKeyword,
            OpenParen = options.OpenParen,
            Options = options.Options,
            CloseParen = options.CloseParen,
            Statement = statement,
        };
    }

    // VACUUM [(option, ...)] [table [(column, ...)], ...] | VACUUM [FULL] [FREEZE] [VERBOSE] [ANALYZE] [table …]
    internal VacuumStatement ParseVacuum()
    {
        var vacuumKeyword = Advance();
        var options = _current.Kind == SyntaxKind.OpenParen
            ? ParseUtilityOptionList()
            : ParseLegacyUtilityOptions(isVacuum: true);
        var tables = ParseMaintenanceTables();
        return new VacuumStatement
        {
            Span = SourceSpan.From(vacuumKeyword, MaintenanceEnd(vacuumKeyword, options, tables)),
            VacuumKeyword = vacuumKeyword,
            OpenParen = options.OpenParen,
            Options = options.Options,
            CloseParen = options.CloseParen,
            Tables = tables,
        };
    }

    // ANALYZE | ANALYSE [(option, ...)] [VERBOSE] [table [(column, ...)], ...]
    internal AnalyzeStatement ParseAnalyze()
    {
        var analyzeKeyword = Advance();
        var options = _current.Kind == SyntaxKind.OpenParen
            ? ParseUtilityOptionList()
            : ParseLegacyUtilityOptions(isVacuum: false);
        var tables = ParseMaintenanceTables();
        return new AnalyzeStatement
        {
            Span = SourceSpan.From(analyzeKeyword, MaintenanceEnd(analyzeKeyword, options, tables)),
            AnalyzeKeyword = analyzeKeyword,
            OpenParen = options.OpenParen,
            Options = options.Options,
            CloseParen = options.CloseParen,
            Tables = tables,
        };
    }

    // CLUSTER [(option, ...)] [VERBOSE] [table [USING index]]
    internal ClusterStatement ParseCluster()
    {
        var clusterKeyword = Advance();
        var options = _current.Kind == SyntaxKind.OpenParen
            ? ParseUtilityOptionList()
            : ParseLegacyUtilityOptions(isVacuum: false);
        var end = MaintenanceEnd(clusterKeyword, options, []);
        IReadOnlyList<SyntaxToken>? table = null;
        SyntaxToken? usingKeyword = null;
        SyntaxToken? index = null;
        if (_current.Kind == SyntaxKind.Identifier)
        {
            var tableName = ParseQualifiedName();
            table = tableName;
            end = tableName[^1].Span;
            if (_current.Kind == SyntaxKind.UsingKeyword)
            {
                usingKeyword = Advance();
                var indexName = Expect(SyntaxKind.Identifier);
                index = indexName;
                end = indexName.Span;
            }
        }

        return new ClusterStatement
        {
            Span = SourceSpan.From(clusterKeyword, end),
            ClusterKeyword = clusterKeyword,
            OpenParen = options.OpenParen,
            Options = options.Options,
            CloseParen = options.CloseParen,
            Table = table,
            UsingKeyword = usingKeyword,
            Index = index,
        };
    }

    private static SourceSpan MaintenanceEnd(
        SyntaxToken keyword,
        UtilityOptionList options,
        IReadOnlyList<MaintenanceTable> tables)
    {
        if (tables.Count > 0)
        {
            return tables[^1].Span;
        }

        if (options.CloseParen is SyntaxToken closeParen)
        {
            return closeParen.Span;
        }

        return options.Options.Count > 0 ? options.Options[^1].Span : keyword.Span;
    }

    // EXPLAIN (…) wraps a statement that may itself start with '(' — only an identifier option name
    // right after the paren makes it an option list.
    private bool IsUtilityOptionListStart() =>
        _current.Kind == SyntaxKind.OpenParen && NextKind is SyntaxKind.Identifier or SyntaxKind.FullKeyword
            && !NextEquals(Keyword.Table);

    // ( option [value] [, ...] ) as used by EXPLAIN, VACUUM, ANALYZE, CLUSTER and DROP DATABASE.
    internal UtilityOptionList ParseUtilityOptionList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var options = new List<KeywordOption> { ParseUtilityOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseUtilityOption());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new UtilityOptionList(openParen, options, closeParen);
    }

    private KeywordOption ParseUtilityOption()
    {
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.FullKeyword))
        {
            throw new SqlParseException($"Expected option, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        if (_current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen)
        {
            return new KeywordOption { Span = name.Span, Keywords = [name] };
        }

        var value = ParseOptionValue();
        return new KeywordOption
        {
            Span = SourceSpan.From(name, value.Span),
            Keywords = [name],
            Value = value,
        };
    }

    // The unparenthesized keyword options: ANALYZE/ANALYSE and VERBOSE everywhere, FULL and FREEZE for VACUUM.
    private UtilityOptionList ParseLegacyUtilityOptions(bool isVacuum)
    {
        List<KeywordOption>? options = null;
        while (IsLegacyUtilityOption(isVacuum))
        {
            var word = Advance();
            (options ??= []).Add(new KeywordOption { Span = word.Span, Keywords = [word] });
        }

        return new UtilityOptionList(null, options ?? (IReadOnlyList<KeywordOption>)[], null);
    }

    private bool IsLegacyUtilityOption(bool isVacuum) =>
        IdentifierEquals(Keyword.UtilVerbose) || IdentifierEquals(Keyword.UtilAnalyze) || IdentifierEquals(Keyword.UtilAnalyse)
        || (isVacuum && (_current.Kind == SyntaxKind.FullKeyword || IdentifierEquals(Keyword.UtilFreeze)));

    private IReadOnlyList<MaintenanceTable> ParseMaintenanceTables()
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            return [];
        }

        var tables = new List<MaintenanceTable> { ParseMaintenanceTable() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            tables.Add(ParseMaintenanceTable());
        }

        return tables;
    }

    private MaintenanceTable ParseMaintenanceTable()
    {
        var name = ParseQualifiedName();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new MaintenanceTable { Span = SourceSpan.From(name[0], name[^1]), Name = name };
        }

        var openParen = Advance();
        var columns = ParseNameList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MaintenanceTable
        {
            Span = SourceSpan.From(name[0], closeParen),
            Name = name,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }
}
