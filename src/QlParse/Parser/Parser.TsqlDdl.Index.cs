namespace QlParse;

// SQL Server indexes and statistics (workstream D): rowstore CREATE INDEX with INCLUDE / WITH / ON storage,
// columnstore / XML / spatial / JSON / vector indexes, ALTER INDEX, DROP INDEX, and CREATE / UPDATE STATISTICS.
internal sealed partial class Parser
{
    // `CREATE [UNIQUE] [CLUSTERED|NONCLUSTERED] INDEX name ON table (columns) [INCLUDE (...)] [WHERE filter]
    // [WITH (...)] [ON storage] [FILESTREAM_ON fg]`.
    private CreateIndexStatement ParseTsqlCreateIndex()
    {
        var createKeyword = Advance();
        var unique = _current.Kind == SyntaxKind.UniqueKeyword ? Advance() : (SyntaxToken?)null;
        var clustered = IdentifierEquals(Keyword.Clustered) || IdentifierEquals(Keyword.Nonclustered) ? Advance() : (SyntaxToken?)null;
        var indexKeyword = ExpectIdent(Keyword.Index);
        var name = ParseQualifiedName();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var tableName = ParseQualifiedName();
        var columns = ParseTsqlIndexColumns();
        var include = TryParseTsqlInclude();
        var (whereKeyword, where) = TryParseTsqlFilter();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        var storage = TryParseTsqlOnStorage();
        var filestreamOn = TryParseTsqlFilestreamOn();
        var hasExtras = include is not null || options is not null || storage is not null || filestreamOn is not null;
        return new CreateIndexStatement
        {
            Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
            CreateKeyword = createKeyword,
            UniqueKeyword = unique,
            ClusteredKeyword = clustered,
            IndexKeyword = indexKeyword,
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            OpenParen = columns.OpenParen,
            Columns = columns.Columns,
            CloseParen = columns.CloseParen,
            WhereKeyword = whereKeyword,
            Where = where,
            SqlServer = hasExtras
                ? new TsqlCreateIndexExtras { Include = include, WithKeyword = withKeyword, Options = options, Storage = storage, FilestreamOn = filestreamOn }
                : null,
        };
    }

    private readonly record struct TsqlSpecialIndexHead(
        SyntaxToken CreateKeyword,
        SyntaxToken? PrimaryKeyword,
        SyntaxToken? ClusteredKeyword,
        SyntaxToken KindKeyword,
        SyntaxToken IndexKeyword,
        IReadOnlyList<SyntaxToken> Name,
        SyntaxToken OnKeyword,
        IReadOnlyList<SyntaxToken> TableName);

    private readonly record struct TsqlNameListClause(SyntaxToken? Keyword, SyntaxToken? OpenParen, IReadOnlyList<SyntaxToken>? Names, SyntaxToken? CloseParen);

    private readonly record struct TsqlXmlIndexUsing(
        SyntaxToken? UsingKeyword,
        SyntaxToken? XmlKeyword,
        SyntaxToken? IndexKeyword,
        SyntaxToken? IndexName,
        SyntaxToken? Tessellation,
        SyntaxToken? ForKeyword,
        SyntaxToken? SecondaryKind);

    // CREATE of a columnstore, XML, spatial, JSON or vector index.
    private TsqlCreateSpecialIndexStatement ParseTsqlCreateSpecialIndex()
    {
        var createKeyword = Advance();
        var primary = IdentifierEquals(Keyword.Primary) ? Advance() : (SyntaxToken?)null;
        var clustered = IdentifierEquals(Keyword.Clustered) || IdentifierEquals(Keyword.Nonclustered) ? Advance() : (SyntaxToken?)null;
        var kindKeyword = Advance();
        var indexKeyword = ExpectIdent(Keyword.Index);
        var name = ParseQualifiedName();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var head = new TsqlSpecialIndexHead(createKeyword, primary, clustered, kindKeyword, indexKeyword, name, onKeyword, ParseQualifiedName());
        TsqlIndexColumnList? columns = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlIndexColumns() : null;
        var order = TryParseTsqlNameListClause(_current.Kind == SyntaxKind.OrderKeyword);
        var usingClause = ParseTsqlSpecialIndexUsing();
        var paths = usingClause.ForKeyword is null && _current.Kind == SyntaxKind.ForKeyword ? ParseTsqlJsonIndexPaths() : default;
        var (whereKeyword, where) = TryParseTsqlFilter();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        var tail = new TsqlSpecialIndexTail(whereKeyword, where, withKeyword, options, TryParseTsqlOnStorage());
        return BuildTsqlSpecialIndex(head, columns, order, usingClause, paths, tail, SourceSpan.From(createKeyword, TsqlLastConsumed()));
    }

    private readonly record struct TsqlSpecialIndexTail(
        SyntaxToken? WhereKeyword,
        Expression? Where,
        SyntaxToken? WithKeyword,
        TsqlRelationalOptionList? Options,
        TsqlStorageClause? Storage);

    private static TsqlCreateSpecialIndexStatement BuildTsqlSpecialIndex(
        TsqlSpecialIndexHead head,
        TsqlIndexColumnList? columns,
        TsqlNameListClause order,
        TsqlXmlIndexUsing usingClause,
        TsqlNameListClause paths,
        TsqlSpecialIndexTail tail,
        SourceSpan span) => new()
    {
        Span = span,
        CreateKeyword = head.CreateKeyword,
        PrimaryKeyword = head.PrimaryKeyword,
        ClusteredKeyword = head.ClusteredKeyword,
        KindKeyword = head.KindKeyword,
        IndexKeyword = head.IndexKeyword,
        Name = head.Name,
        OnKeyword = head.OnKeyword,
        TableName = head.TableName,
        OpenParen = columns?.OpenParen,
        Columns = columns?.Columns,
        CloseParen = columns?.CloseParen,
        OrderKeyword = order.Keyword,
        OrderOpen = order.OpenParen,
        OrderColumns = order.Names,
        OrderClose = order.CloseParen,
        UsingKeyword = usingClause.UsingKeyword,
        UsingXmlKeyword = usingClause.XmlKeyword,
        UsingIndexKeyword = usingClause.IndexKeyword,
        UsingIndexName = usingClause.IndexName,
        Tessellation = usingClause.Tessellation,
        ForKeyword = usingClause.ForKeyword ?? paths.Keyword,
        SecondaryKind = usingClause.SecondaryKind,
        ForOpen = paths.OpenParen,
        Paths = paths.Names,
        ForClose = paths.CloseParen,
        WhereKeyword = tail.WhereKeyword,
        Where = tail.Where,
        WithKeyword = tail.WithKeyword,
        Options = tail.Options,
        Storage = tail.Storage,
    };

    // `keyword (name, ...)` when present is true (columnstore ORDER).
    private TsqlNameListClause TryParseTsqlNameListClause(bool present)
    {
        if (!present)
        {
            return default;
        }

        var keyword = Advance();
        ParseRequiredNameList(out var openParen, out var names, out var closeParen);
        return new TsqlNameListClause(keyword, openParen, names, closeParen);
    }

    // `USING XML INDEX name [FOR {VALUE|PATH|PROPERTY}]` (secondary XML) or `USING tessellation` (spatial).
    private TsqlXmlIndexUsing ParseTsqlSpecialIndexUsing()
    {
        if (_current.Kind != SyntaxKind.UsingKeyword)
        {
            return default;
        }

        var usingKeyword = Advance();
        if (!IdentifierEquals(Keyword.Xml) || !NextEquals(Keyword.Index))
        {
            return new TsqlXmlIndexUsing(usingKeyword, null, null, null, Expect(SyntaxKind.Identifier), null, null);
        }

        var xmlKeyword = Advance();
        var indexKeyword = Advance();
        var indexName = Expect(SyntaxKind.Identifier);
        if (_current.Kind != SyntaxKind.ForKeyword)
        {
            return new TsqlXmlIndexUsing(usingKeyword, xmlKeyword, indexKeyword, indexName, null, null, null);
        }

        var forKeyword = Advance();
        var secondaryKind = _current.Kind == SyntaxKind.ValuesKeyword ? Advance() : Expect(SyntaxKind.Identifier);
        return new TsqlXmlIndexUsing(usingKeyword, xmlKeyword, indexKeyword, indexName, null, forKeyword, secondaryKind);
    }

    // JSON index paths: `FOR ('$.a', '$.b')`.
    private TsqlNameListClause ParseTsqlJsonIndexPaths()
    {
        var forKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var paths = new List<SyntaxToken> { Expect(SyntaxKind.String) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            paths.Add(Expect(SyntaxKind.String));
        }

        return new TsqlNameListClause(forKeyword, openParen, paths, Expect(SyntaxKind.CloseParen));
    }

    // `ALTER INDEX {name|ALL} ON table {REBUILD|REORGANIZE|DISABLE|RESUME|PAUSE|ABORT|SET (...)}
    // [PARTITION = {ALL|n}] [WITH (...)]`.
    private TsqlAlterIndexStatement ParseTsqlAlterIndex()
    {
        var alterKeyword = Advance();
        var indexKeyword = Advance();
        var allKeyword = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<SyntaxToken> name = allKeyword is null ? ParseQualifiedName() : [];
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var tableName = ParseQualifiedName();
        if (!IsTsqlAlterIndexAction())
        {
            throw new SqlParseException($"Expected ALTER INDEX action, found {_current.Kind}", _current.Position);
        }

        var actionKeyword = Advance();
        var (partitionKeyword, equalsToken, partition) = TryParseTsqlPartitionAssignment();
        SyntaxToken? withKeyword = null;
        TsqlRelationalOptionList? options;
        if (actionKeyword.Kind == SyntaxKind.SetKeyword)
        {
            options = ParseTsqlRelationalOptions();
        }
        else
        {
            var (optionsWith, optionList) = TryParseTsqlWithOptionsClause();
            withKeyword = optionsWith;
            options = optionList;
        }

        return new TsqlAlterIndexStatement
        {
            Span = SourceSpan.From(alterKeyword, TsqlLastConsumed()),
            AlterKeyword = alterKeyword,
            IndexKeyword = indexKeyword,
            Name = name,
            AllKeyword = allKeyword,
            OnKeyword = onKeyword,
            TableName = tableName,
            ActionKeyword = actionKeyword,
            PartitionKeyword = partitionKeyword,
            EqualsToken = equalsToken,
            Partition = partition,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private bool IsTsqlAlterIndexAction() =>
        _current.Kind == SyntaxKind.SetKeyword
        || IdentifierEquals(Keyword.Rebuild)
        || IdentifierEquals(Keyword.Reorganize)
        || IdentifierEquals(Keyword.Disable)
        || IdentifierEquals(Keyword.TsqlDdlResume)
        || IdentifierEquals(Keyword.TsqlDdlPause)
        || IdentifierEquals(Keyword.TsqlDdlAbort);

    // `DROP INDEX [IF EXISTS] {name ON table [WITH (...)] | table.name} [, ...]`.
    private TsqlDropIndexStatement ParseTsqlDropIndex()
    {
        var dropKeyword = Advance();
        var indexKeyword = Advance();
        var ifExists = TryParseTsqlIfExists();
        var items = new List<TsqlDropIndexItem> { ParseTsqlDropIndexItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseTsqlDropIndexItem());
        }

        return new TsqlDropIndexStatement
        {
            Span = SourceSpan.From(dropKeyword, items[^1].Span),
            DropKeyword = dropKeyword,
            IndexKeyword = indexKeyword,
            IfExists = ifExists,
            Items = items,
        };
    }

    private TsqlDropIndexItem ParseTsqlDropIndexItem()
    {
        var name = ParseQualifiedName();
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return new TsqlDropIndexItem { Span = SourceSpan.From(name[0], name[^1]), Name = name };
        }

        var onKeyword = Advance();
        var tableName = ParseQualifiedName();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlDropIndexItem
        {
            Span = SourceSpan.From(name[0], TsqlLastConsumed()),
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // `CREATE STATISTICS name ON table (columns) [WHERE filter] [WITH options]`.
    private TsqlCreateStatisticsStatement ParseTsqlCreateStatistics()
    {
        var createKeyword = Advance();
        var statisticsKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var tableName = ParseQualifiedName();
        ParseRequiredNameList(out var openParen, out var columns, out var closeParen);
        var (whereKeyword, where) = TryParseTsqlFilter();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlCreateStatisticsStatement
        {
            Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
            CreateKeyword = createKeyword,
            StatisticsKeyword = statisticsKeyword,
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            WhereKeyword = whereKeyword,
            Where = where,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // `UPDATE STATISTICS table [name | (name, ...)] [WITH options]`; a reserved word after the table starts
    // the next statement rather than naming a statistic.
    private TsqlUpdateStatisticsStatement ParseTsqlUpdateStatistics()
    {
        var updateKeyword = Advance();
        var statisticsKeyword = Advance();
        var tableName = ParseQualifiedName();
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken> statistics = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseRequiredNameList(out var open, out var names, out var close);
            openParen = open;
            statistics = names;
            closeParen = close;
        }
        else
        {
            if (_current.Kind == SyntaxKind.Identifier && !IsTsqlReservedAliasWord())
            {
                statistics = [Advance()];
            }
        }

        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlUpdateStatisticsStatement
        {
            Span = SourceSpan.From(updateKeyword, TsqlLastConsumed()),
            UpdateKeyword = updateKeyword,
            StatisticsKeyword = statisticsKeyword,
            TableName = tableName,
            OpenParen = openParen,
            Statistics = statistics,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            Options = options,
        };
    }
}
