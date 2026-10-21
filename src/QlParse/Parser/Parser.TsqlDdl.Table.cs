namespace QlParse;

// SQL Server CREATE TABLE (workstream D): the element list (columns with their T-SQL properties, constraints,
// inline indexes, PERIOD FOR SYSTEM_TIME) and the table tail (AS NODE/EDGE, storage, WITH options).
internal sealed partial class Parser
{
    private readonly record struct TsqlElementList(SyntaxToken OpenParen, IReadOnlyList<TableElement> Elements, SyntaxToken CloseParen);

    // The last token the parser consumed; T-SQL clauses end wherever their optional parts stopped.
    private SyntaxToken TsqlLastConsumed() => _tokens[_index - Len2];

    private CreateTableStatement ParseTsqlCreateTable()
    {
        var createKeyword = Advance();
        var tableKeyword = Advance();
        var name = ParseQualifiedName();
        TsqlElementList? body = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlTableElements() : null;
        var extras = ParseTsqlCreateTableTail();
        return new CreateTableStatement
        {
            Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
            CreateKeyword = createKeyword,
            TableKeyword = tableKeyword,
            Name = name,
            OpenParen = body?.OpenParen,
            Elements = body?.Elements,
            CloseParen = body?.CloseParen,
            SqlServer = extras,
        };
    }

    private TsqlElementList ParseTsqlTableElements()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = new List<TableElement> { ParseTsqlTableElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseTsqlTableElement());
        }

        return new TsqlElementList(openParen, elements, Expect(SyntaxKind.CloseParen));
    }

    private TableElement ParseTsqlTableElement()
    {
        if (IsTsqlTableConstraintStart())
        {
            return ParseTsqlTableConstraint();
        }

        if (IsPeriodDefinition())
        {
            return ParsePeriodDefinition();
        }

        return IsTsqlInlineIndexStart() ? ParseTsqlTableIndex() : ParseTsqlColumn();
    }

    private bool IsTsqlInlineIndexStart() => IdentifierEquals(Keyword.Index) && NextKind == SyntaxKind.Identifier;

    private bool IsTsqlTableConstraintStart() =>
        IsTableConstraintStart() || IdentifierEquals(Keyword.Connection);

    // `[AS {NODE|EDGE|FILETABLE}] [ON storage] [TEXTIMAGE_ON fg] [FILESTREAM_ON fg] [WITH (options)]`.
    private TsqlCreateTableExtras? ParseTsqlCreateTableTail()
    {
        SyntaxToken? asKeyword = null;
        SyntaxToken? tableKind = null;
        if (_current.Kind == SyntaxKind.AsKeyword && IsTsqlTableKindNext())
        {
            asKeyword = Advance();
            tableKind = Advance();
        }

        var storage = TryParseTsqlOnStorage();
        var textImageOn = IdentifierEquals(Keyword.TsqlDdlTextImageOn) ? ParseTsqlStorageClause() : null;
        var filestreamOn = TryParseTsqlFilestreamOn();
        var (withKeyword, options) = TryParseTsqlParenthesizedWith();
        storage ??= TryParseTsqlOnStorage();
        return tableKind is null && storage is null && IsTsqlTableTailEmpty(textImageOn, filestreamOn, options)
            ? null
            : new TsqlCreateTableExtras
        {
            AsKeyword = asKeyword,
            TableKind = tableKind,
            Storage = storage,
            TextImageOn = textImageOn,
            FilestreamOn = filestreamOn,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // `WITH (options)`; a WITH not followed by `(` is left alone.
    private (SyntaxToken? WithKeyword, TsqlRelationalOptionList? Options) TryParseTsqlParenthesizedWith()
    {
        if (_current.Kind != SyntaxKind.WithKeyword || NextKind != SyntaxKind.OpenParen)
        {
            return (null, null);
        }

        var withKeyword = Advance();
        return (withKeyword, ParseTsqlRelationalOptions());
    }

    private static bool IsTsqlTableTailEmpty(TsqlStorageClause? textImageOn, TsqlStorageClause? filestreamOn, TsqlRelationalOptionList? options) =>
        textImageOn is null && filestreamOn is null && options is null;

    private bool IsTsqlTableKindNext() =>
        NextEquals(Keyword.TsqlDdlNode) || NextEquals(Keyword.Edge) || NextEquals(Keyword.TsqlDdlFiletable);

    // ---- Columns ----

    // Accumulates the parts of one T-SQL column definition while its options are parsed in any order.
    private sealed class TsqlColumnParts
    {
        internal DefaultClause? Default { get; set; }
        internal SyntaxToken? DefaultConstraintKeyword { get; set; }
        internal SyntaxToken? DefaultConstraintName { get; set; }
        internal SyntaxToken? DefaultWithKeyword { get; set; }
        internal SyntaxToken? DefaultValuesKeyword { get; set; }
        internal GeneratedColumn? Generated { get; set; }
        internal SyntaxToken? CollateKeyword { get; set; }
        internal IReadOnlyList<SyntaxToken>? Collation { get; set; }
        internal TsqlComputedColumn? Computed { get; set; }
        internal TsqlIdentitySpec? Identity { get; set; }
        internal SyntaxToken? Filestream { get; set; }
        internal SyntaxToken? Sparse { get; set; }
        internal SyntaxToken? Rowguidcol { get; set; }
        internal SyntaxToken? Hidden { get; set; }
        internal SyntaxToken? ColumnSet { get; set; }
        internal SyntaxToken? ColumnSetFor { get; set; }
        internal SyntaxToken? AllSparseColumns { get; set; }
        internal TsqlMaskingClause? Masking { get; set; }
        internal TsqlEncryptionClause? Encryption { get; set; }
        internal TsqlTableIndex? Index { get; set; }
        internal List<TableConstraint>? Constraints { get; set; }

        internal bool HasExtras => HasValueExtras || HasFlagExtras || HasClauseExtras;

        private bool HasValueExtras =>
            Computed is not null || Identity is not null || DefaultConstraintKeyword is not null || DefaultWithKeyword is not null;

        private bool HasFlagExtras =>
            Filestream is not null || Sparse is not null || Rowguidcol is not null || Hidden is not null;

        private bool HasClauseExtras =>
            ColumnSet is not null || Masking is not null || Encryption is not null || Index is not null;
    }

    private ColumnDefinition ParseTsqlColumn()
    {
        var name = Expect(SyntaxKind.Identifier);
        var parts = new TsqlColumnParts();
        DataType? type = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            parts.Computed = ParseTsqlComputedColumn();
        }
        else
        {
            type = ParseDataType();
        }

        while (TryParseTsqlColumnValueOption(parts) || TryParseTsqlColumnFlagOption(parts) || TryParseTsqlColumnConstraint(parts))
        {
        }

        return new ColumnDefinition
        {
            Span = SourceSpan.From(name, TsqlLastConsumed()),
            Name = name,
            Type = type,
            Default = parts.Default,
            Generated = parts.Generated,
            CollateKeyword = parts.CollateKeyword,
            Collation = parts.Collation,
            Constraints = parts.Constraints ?? (IReadOnlyList<TableConstraint>)[],
            SqlServer = parts.HasExtras ? BuildTsqlColumnExtras(parts) : null,
        };
    }

    private static TsqlColumnExtras BuildTsqlColumnExtras(TsqlColumnParts parts) => new()
    {
        Computed = parts.Computed,
        Identity = parts.Identity,
        FilestreamKeyword = parts.Filestream,
        SparseKeyword = parts.Sparse,
        RowguidcolKeyword = parts.Rowguidcol,
        HiddenKeyword = parts.Hidden,
        ColumnSetKeyword = parts.ColumnSet,
        ColumnSetForKeyword = parts.ColumnSetFor,
        AllSparseColumnsKeyword = parts.AllSparseColumns,
        Masking = parts.Masking,
        Encryption = parts.Encryption,
        DefaultConstraintKeyword = parts.DefaultConstraintKeyword,
        DefaultConstraintName = parts.DefaultConstraintName,
        DefaultWithKeyword = parts.DefaultWithKeyword,
        DefaultValuesKeyword = parts.DefaultValuesKeyword,
        Index = parts.Index,
    };

    private TsqlComputedColumn ParseTsqlComputedColumn()
    {
        var asKeyword = Advance();
        var expression = ParseExpression();
        var persisted = IdentifierEquals(Keyword.TsqlDdlPersisted) ? Advance() : (SyntaxToken?)null;
        return new TsqlComputedColumn
        {
            Span = SourceSpan.From(asKeyword, persisted?.Span ?? expression.Span),
            AsKeyword = asKeyword,
            Expression = expression,
            PersistedKeyword = persisted,
        };
    }

    // DEFAULT, CONSTRAINT name DEFAULT, IDENTITY, GENERATED ALWAYS AS ROW ..., COLLATE.
    private bool TryParseTsqlColumnValueOption(TsqlColumnParts parts)
    {
        if (IdentifierEquals(Keyword.Constraint) && IsKeywordAt(_index + 1, Keyword.Default))
        {
            parts.DefaultConstraintKeyword = Advance();
            parts.DefaultConstraintName = Advance();
            ParseTsqlColumnDefault(parts);
            return true;
        }

        if (IdentifierEquals(Keyword.Default))
        {
            ParseTsqlColumnDefault(parts);
            return true;
        }

        if (IdentifierEquals(Keyword.Identity) && parts.Identity is null)
        {
            parts.Identity = ParseTsqlIdentity();
            return true;
        }

        if (IdentifierEquals(Keyword.Generated) && parts.Generated is null)
        {
            parts.Generated = ParseTsqlGeneratedColumn();
            return true;
        }

        if (_current.Kind == SyntaxKind.CollateKeyword && parts.CollateKeyword is null)
        {
            parts.CollateKeyword = Advance();
            parts.Collation = ParseQualifiedName();
            return true;
        }

        return false;
    }

    private void ParseTsqlColumnDefault(TsqlColumnParts parts)
    {
        if (parts.Default is not null)
        {
            throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
        }

        parts.Default = ParseDefaultClause();
        if (_current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.ValuesKeyword)
        {
            parts.DefaultWithKeyword = Advance();
            parts.DefaultValuesKeyword = Advance();
        }
    }

    // FILESTREAM, SPARSE, ROWGUIDCOL, HIDDEN, COLUMN_SET FOR ALL_SPARSE_COLUMNS, MASKED WITH, ENCRYPTED WITH, INDEX.
    private bool TryParseTsqlColumnFlagOption(TsqlColumnParts parts)
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            return false;
        }

        if (TryParseTsqlColumnKeywordFlag(parts))
        {
            return true;
        }

        if (IdentifierEquals(Keyword.TsqlDdlColumnSet) && parts.ColumnSet is null)
        {
            parts.ColumnSet = Advance();
            parts.ColumnSetFor = Expect(SyntaxKind.ForKeyword);
            parts.AllSparseColumns = ExpectIdent(Keyword.TsqlDdlAllSparseColumns);
            return true;
        }

        return TryParseTsqlColumnClauseFlag(parts);
    }

    private bool TryParseTsqlColumnClauseFlag(TsqlColumnParts parts)
    {
        if (IdentifierEquals(Keyword.TsqlDdlMasked) && parts.Masking is null)
        {
            parts.Masking = ParseTsqlMasking();
            return true;
        }

        if (IdentifierEquals(Keyword.TsqlDdlEncrypted) && parts.Encryption is null)
        {
            parts.Encryption = ParseTsqlEncryption();
            return true;
        }

        if (IsTsqlInlineIndexStart() && parts.Index is null)
        {
            parts.Index = ParseTsqlTableIndex();
            return true;
        }

        return false;
    }

    private bool TryParseTsqlColumnKeywordFlag(TsqlColumnParts parts)
    {
        if (IdentifierEquals(Keyword.TsqlDdlFilestream) && parts.Filestream is null)
        {
            parts.Filestream = Advance();
            return true;
        }

        if (IdentifierEquals(Keyword.TsqlDdlSparse) && parts.Sparse is null)
        {
            parts.Sparse = Advance();
            return true;
        }

        if (IdentifierEquals(Keyword.TsqlDdlRowguidcol) && parts.Rowguidcol is null)
        {
            parts.Rowguidcol = Advance();
            return true;
        }

        if (IdentifierEquals(Keyword.TsqlDdlHidden) && parts.Hidden is null)
        {
            parts.Hidden = Advance();
            return true;
        }

        return false;
    }

    // NULL, NOT NULL and `[CONSTRAINT name] {PRIMARY KEY|UNIQUE|CHECK|[FOREIGN KEY] REFERENCES}` on a column.
    private bool TryParseTsqlColumnConstraint(TsqlColumnParts parts)
    {
        TableConstraint constraint;
        if (_current.Kind == SyntaxKind.NullKeyword || (_current.Kind == SyntaxKind.NotKeyword && NextKind == SyntaxKind.NullKeyword))
        {
            var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
            var nullKeyword = Advance();
            constraint = new TableConstraint
            {
                Span = SourceSpan.From(notKeyword ?? nullKeyword, nullKeyword),
                NotKeyword = notKeyword,
                NullKeyword = nullKeyword,
            };
        }
        else
        {
            if (!IsTsqlTableConstraintStart() && !IdentifierEquals(Keyword.References))
            {
                return false;
            }

            constraint = ParseTsqlConstraint(column: true);
        }

        parts.Constraints ??= [];
        parts.Constraints.Add(constraint);
        return true;
    }

    private TsqlIdentitySpec ParseTsqlIdentity()
    {
        var identityKeyword = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var bareNotForReplication = TryParseTsqlNotForReplication();
            return new TsqlIdentitySpec
            {
                Span = SourceSpan.From(identityKeyword, bareNotForReplication?.Span ?? identityKeyword.Span),
                IdentityKeyword = identityKeyword,
                NotForReplication = bareNotForReplication,
            };
        }

        var openParen = Advance();
        var seed = ParseExpression();
        var comma = Expect(SyntaxKind.Comma);
        var increment = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var notForReplication = TryParseTsqlNotForReplication();
        return new TsqlIdentitySpec
        {
            Span = SourceSpan.From(identityKeyword, notForReplication?.Span ?? closeParen.Span),
            IdentityKeyword = identityKeyword,
            OpenParen = openParen,
            Seed = seed,
            Comma = comma,
            Increment = increment,
            CloseParen = closeParen,
            NotForReplication = notForReplication,
        };
    }

    private TsqlNotForReplication? TryParseTsqlNotForReplication()
    {
        if (_current.Kind != SyntaxKind.NotKeyword || NextKind != SyntaxKind.ForKeyword)
        {
            return null;
        }

        var notKeyword = Advance();
        var forKeyword = Advance();
        var replicationKeyword = ExpectIdent(Keyword.TsqlDdlReplication);
        return new TsqlNotForReplication
        {
            Span = SourceSpan.From(notKeyword, replicationKeyword),
            NotKeyword = notKeyword,
            ForKeyword = forKeyword,
            ReplicationKeyword = replicationKeyword,
        };
    }

    // `GENERATED ALWAYS AS {ROW|TRANSACTION_ID|SEQUENCE_NUMBER} {START|END}`; RowKeyword holds the row kind.
    private GeneratedColumn ParseTsqlGeneratedColumn()
    {
        var generatedKeyword = Advance();
        var alwaysKeyword = ExpectIdent(Keyword.Always);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        if (!IdentifierEquals(Keyword.Row) && !IdentifierEquals(Keyword.TsqlDdlTransactionId) && !IdentifierEquals(Keyword.TsqlDdlSequenceNumber))
        {
            throw new SqlParseException($"Expected ROW, TRANSACTION_ID, or SEQUENCE_NUMBER, found {_current.Kind}", _current.Position);
        }

        var rowKeyword = Advance();
        var bound = _current.Kind == SyntaxKind.EndKeyword ? Advance() : ExpectIdent(Keyword.Start);
        return new GeneratedColumn
        {
            Span = SourceSpan.From(generatedKeyword, bound),
            GeneratedKeyword = generatedKeyword,
            AlwaysKeyword = alwaysKeyword,
            AsKeyword = asKeyword,
            RowKeyword = rowKeyword,
            Bound = bound,
        };
    }

    // `MASKED [WITH (FUNCTION = 'mask()')]`.
    private TsqlMaskingClause ParseTsqlMasking()
    {
        var maskedKeyword = Advance();
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return new TsqlMaskingClause { Span = maskedKeyword.Span, MaskedKeyword = maskedKeyword };
        }

        var withKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var functionKeyword = ExpectIdent(Keyword.Function);
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var function = Expect(SyntaxKind.String);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlMaskingClause
        {
            Span = SourceSpan.From(maskedKeyword, closeParen),
            MaskedKeyword = maskedKeyword,
            WithKeyword = withKeyword,
            OpenParen = openParen,
            FunctionKeyword = functionKeyword,
            EqualsToken = equalsToken,
            Function = function,
            CloseParen = closeParen,
        };
    }

    private TsqlEncryptionClause ParseTsqlEncryption()
    {
        var encryptedKeyword = Advance();
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var options = ParseTsqlRelationalOptions();
        return new TsqlEncryptionClause
        {
            Span = SourceSpan.From(encryptedKeyword, options.Span),
            EncryptedKeyword = encryptedKeyword,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // ---- Inline indexes ----

    // `INDEX name [UNIQUE] [CLUSTERED|NONCLUSTERED] [COLUMNSTORE] [HASH] [(columns)] [INCLUDE (...)]
    // [WHERE filter] [WITH (...)] [ON storage] [FILESTREAM_ON fg]`.
    private TsqlTableIndex ParseTsqlTableIndex()
    {
        var indexKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var unique = _current.Kind == SyntaxKind.UniqueKeyword ? Advance() : (SyntaxToken?)null;
        var clustered = IdentifierEquals(Keyword.Clustered) || IdentifierEquals(Keyword.Nonclustered) ? Advance() : (SyntaxToken?)null;
        var columnstore = IdentifierEquals(Keyword.TsqlDdlColumnstore) ? Advance() : (SyntaxToken?)null;
        var hash = IdentifierEquals(Keyword.TsqlDdlHash) ? Advance() : (SyntaxToken?)null;
        TsqlIndexColumnList? columns = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlIndexColumns() : null;
        var include = TryParseTsqlInclude();
        var (whereKeyword, where) = TryParseTsqlFilter();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        var storage = TryParseTsqlOnStorage();
        var filestreamOn = TryParseTsqlFilestreamOn();
        return new TsqlTableIndex
        {
            Span = SourceSpan.From(indexKeyword, TsqlLastConsumed()),
            IndexKeyword = indexKeyword,
            Name = name,
            UniqueKeyword = unique,
            ClusteredKeyword = clustered,
            ColumnstoreKeyword = columnstore,
            HashKeyword = hash,
            OpenParen = columns?.OpenParen,
            Columns = columns?.Columns,
            CloseParen = columns?.CloseParen,
            Include = include,
            WhereKeyword = whereKeyword,
            Where = where,
            WithKeyword = withKeyword,
            Options = options,
            Storage = storage,
            FilestreamOn = filestreamOn,
        };
    }

    private readonly record struct TsqlIndexColumnList(SyntaxToken OpenParen, IReadOnlyList<IndexColumn> Columns, SyntaxToken CloseParen);

    private TsqlIndexColumnList ParseTsqlIndexColumns()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<IndexColumn> { ParseIndexColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseIndexColumn());
        }

        return new TsqlIndexColumnList(openParen, columns, Expect(SyntaxKind.CloseParen));
    }

    private (SyntaxToken? WhereKeyword, Expression? Where) TryParseTsqlFilter()
    {
        if (_current.Kind != SyntaxKind.WhereKeyword)
        {
            return (null, null);
        }

        var whereKeyword = Advance();
        return (whereKeyword, ParseExpression());
    }
}
