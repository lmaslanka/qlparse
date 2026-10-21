namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct PgTableTail(
        SyntaxToken? InheritsKeyword,
        SyntaxToken? InheritsOpen,
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? Inherits,
        SyntaxToken? InheritsClose,
        PartitionByClause? PartitionBy,
        SyntaxToken? UsingKeyword,
        SyntaxToken? AccessMethod,
        SyntaxToken? StorageWithKeyword,
        StorageParameterList? StorageParameters,
        SyntaxToken? WithoutKeyword,
        SyntaxToken? OidsKeyword,
        SyntaxToken? OnKeyword,
        SyntaxToken? CommitKeyword,
        SyntaxToken? CommitAction,
        SyntaxToken? RowsKeyword,
        SyntaxToken? TablespaceKeyword,
        SyntaxToken? Tablespace,
        SourceSpan? End);

    private readonly record struct PgInherits(
        SyntaxToken? Keyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? Parents,
        SyntaxToken? CloseParen);

    private readonly record struct PgTableStorage(
        SyntaxToken? WithKeyword,
        StorageParameterList? Parameters,
        SyntaxToken? WithoutKeyword,
        SyntaxToken? OidsKeyword,
        SourceSpan? End);

    // `[INHERITS (...)] [PARTITION BY ...] [USING method] [WITH (...) | WITHOUT OIDS]
    //  [ON COMMIT ...] [TABLESPACE name]`, the Postgres CREATE TABLE tail. Called both before the
    // table body (CREATE TABLE ... AS puts storage clauses before AS) and after it.
    internal PgTableTail ParsePgTableTail()
    {
        var inherits = ParsePgInherits();
        var partitionBy = IsPartitionByStart() ? ParsePartitionBy() : null;
        var (usingKeyword, accessMethod) = TryParsePgUsingMethod();
        var storage = ParsePgTableStorage();
        ParseOnCommit(out var onKeyword, out var commitKeyword, out var commitAction, out var rowsKeyword);
        var (tablespaceKeyword, tablespace) = TryParsePgTablespaceClause();
        var end = tablespace?.Span
            ?? rowsKeyword?.Span
            ?? commitAction?.Span
            ?? storage.End
            ?? accessMethod?.Span
            ?? partitionBy?.Span
            ?? inherits.CloseParen?.Span;
        return new PgTableTail(
            inherits.Keyword,
            inherits.OpenParen,
            inherits.Parents,
            inherits.CloseParen,
            partitionBy,
            usingKeyword,
            accessMethod,
            storage.WithKeyword,
            storage.Parameters,
            storage.WithoutKeyword,
            storage.OidsKeyword,
            onKeyword,
            commitKeyword,
            commitAction,
            rowsKeyword,
            tablespaceKeyword,
            tablespace,
            end);
    }

    // Null unless at least one Postgres-only CREATE TABLE clause is present.
    internal static PgCreateTableExtras? BuildPgCreateTableExtras(
        SyntaxToken? unlogged,
        ExistenceCheck? ifNotExists,
        PartitionOfClause? partitionOf,
        PgTableTail tail)
    {
        if (unlogged is null && ifNotExists is null && partitionOf is null && !HasPgTableClauses(tail))
        {
            return null;
        }

        return new PgCreateTableExtras
        {
            UnloggedKeyword = unlogged,
            IfNotExists = ifNotExists,
            PartitionOf = partitionOf,
            InheritsKeyword = tail.InheritsKeyword,
            InheritsOpen = tail.InheritsOpen,
            Inherits = tail.Inherits,
            InheritsClose = tail.InheritsClose,
            PartitionBy = tail.PartitionBy,
            UsingKeyword = tail.UsingKeyword,
            AccessMethod = tail.AccessMethod,
            StorageWithKeyword = tail.StorageWithKeyword,
            StorageParameters = tail.StorageParameters,
            WithoutKeyword = tail.WithoutKeyword,
            OidsKeyword = tail.OidsKeyword,
            TablespaceKeyword = tail.TablespaceKeyword,
            Tablespace = tail.Tablespace,
        };
    }

    // ON COMMIT is ISO and stays on the statement, so it does not count here.
    private static bool HasPgTableClauses(PgTableTail tail) =>
        tail.InheritsKeyword is not null
        || tail.PartitionBy is not null
        || tail.UsingKeyword is not null
        || tail.StorageWithKeyword is not null
        || tail.WithoutKeyword is not null
        || tail.TablespaceKeyword is not null;

    private PgInherits ParsePgInherits()
    {
        if (!IdentifierEquals(Keyword.Inherits))
        {
            return default;
        }

        var inheritsKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var parents = ParseSchemaNameList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgInherits(inheritsKeyword, openParen, parents, closeParen);
    }

    private PgTableStorage ParsePgTableStorage()
    {
        if (IsPgWithParameters())
        {
            var (withKeyword, parameters) = TryParsePgWithParameters();
            return new PgTableStorage(withKeyword, parameters, null, null, parameters?.Span);
        }

        if (!IdentifierEquals(Keyword.Without) || !NextEquals(Keyword.Oids))
        {
            return default;
        }

        var withoutKeyword = Advance();
        var oidsKeyword = Advance();
        return new PgTableStorage(null, null, withoutKeyword, oidsKeyword, oidsKeyword.Span);
    }

    // Postgres LIKE options: `INCLUDING ALL`, `EXCLUDING INDEXES`, `INCLUDING CONSTRAINTS`, and so on.
    internal bool IsPgLikeOption() =>
        IsPostgres && _current.Kind is SyntaxKind.Identifier or SyntaxKind.AllKeyword;

    internal static SourceSpan MaxEnd(SourceSpan end, SourceSpan? other) =>
        other is { } candidate && candidate.End > end.End ? candidate : end;

    private bool IsPartitionByStart() =>
        IdentifierEquals(Keyword.Partition) && NextKind == SyntaxKind.ByKeyword;

    internal bool IsPartitionOfStart() =>
        IdentifierEquals(Keyword.Partition) && NextKind == SyntaxKind.OfKeyword;

    // `PARTITION BY {RANGE | LIST | HASH} ({column | (expression)} [COLLATE c] [opclass] [, ...])`.
    private PartitionByClause ParsePartitionBy()
    {
        var partitionKeyword = Advance();
        var byKeyword = Advance();
        var strategy = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<IndexColumn> { ParseIndexColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseIndexColumn());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PartitionByClause
        {
            Span = SourceSpan.From(partitionKeyword, closeParen),
            PartitionKeyword = partitionKeyword,
            ByKeyword = byKeyword,
            Strategy = strategy,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    // `PARTITION OF parent [( column [WITH OPTIONS] [constraints] | table_constraint, ... )]
    //  {FOR VALUES ... | DEFAULT}`.
    private TableBody ParsePgPartitionOfBody()
    {
        var partitionKeyword = Advance();
        var ofKeyword = Advance();
        var parent = ParseQualifiedName();
        SyntaxToken? openParen = null;
        IReadOnlyList<TableElement>? elements = null;
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            var items = new List<TableElement> { ParsePgUntypedElement() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                items.Add(ParsePgUntypedElement());
            }

            elements = items;
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var bound = ParsePartitionBound();
        var partitionOf = new PartitionOfClause
        {
            Span = SourceSpan.From(partitionKeyword, bound.Span),
            PartitionKeyword = partitionKeyword,
            OfKeyword = ofKeyword,
            Parent = parent,
            Bound = bound,
        };
        return new TableBody(null, null, null, null, openParen, elements, closeParen, null, null, null, null, null, null, null, null, partitionOf);
    }

    // A partition's element list: constraints, or columns without a data type.
    private TableElement ParsePgUntypedElement()
    {
        if (IsTableConstraintStart() || IsPgExcludeStart())
        {
            return ParseTableConstraint();
        }

        if (IsWithOptions())
        {
            return ParseColumn(withOptions: true);
        }

        var name = Expect(SyntaxKind.Identifier);
        var tail = ParseColumnTail();
        return new ColumnDefinition
        {
            Span = SourceSpan.From(name, ParseColumnEnd(name, null, null, tail)),
            Name = name,
            Default = tail.DefaultClause,
            Identity = tail.Identity,
            Generated = tail.Generated,
            CollateKeyword = tail.CollateKeyword,
            Collation = tail.Collation,
            ScopeKeyword = tail.ScopeKeyword,
            ScopeName = tail.ScopeName,
            Constraints = tail.Constraints,
        };
    }

    internal bool IsPartitionBoundStart() =>
        (_current.Kind == SyntaxKind.ForKeyword && NextKind == SyntaxKind.ValuesKeyword) || IdentifierEquals(Keyword.Default);

    // `FOR VALUES IN (...)` | `FOR VALUES FROM (...) TO (...)` | `FOR VALUES WITH (MODULUS m, REMAINDER r)` | `DEFAULT`.
    internal PartitionBound ParsePartitionBound()
    {
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            return new PartitionBound { Span = defaultKeyword.Span, DefaultKeyword = defaultKeyword };
        }

        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            var withKeyword = Advance();
            var hashParameters = ParseStorageParameterList();
            return new PartitionBound
            {
                Span = SourceSpan.From(forKeyword, hashParameters.Span),
                ForKeyword = forKeyword,
                ValuesKeyword = valuesKeyword,
                WithKeyword = withKeyword,
                HashParameters = hashParameters,
            };
        }

        if (_current.Kind == SyntaxKind.InKeyword)
        {
            var inKeyword = Advance();
            var openParen = Expect(SyntaxKind.OpenParen);
            var values = ParseExpressionList();
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new PartitionBound
            {
                Span = SourceSpan.From(forKeyword, closeParen),
                ForKeyword = forKeyword,
                ValuesKeyword = valuesKeyword,
                InKeyword = inKeyword,
                OpenParen = openParen,
                Values = values,
                CloseParen = closeParen,
            };
        }

        return ParseRangePartitionBound(forKeyword, valuesKeyword);
    }

    private PartitionBound ParseRangePartitionBound(SyntaxToken forKeyword, SyntaxToken valuesKeyword)
    {
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var values = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var toKeyword = ExpectIdent(Keyword.To);
        var toOpen = Expect(SyntaxKind.OpenParen);
        var toValues = ParseExpressionList();
        var toClose = Expect(SyntaxKind.CloseParen);
        return new PartitionBound
        {
            Span = SourceSpan.From(forKeyword, toClose),
            ForKeyword = forKeyword,
            ValuesKeyword = valuesKeyword,
            FromKeyword = fromKeyword,
            OpenParen = openParen,
            Values = values,
            CloseParen = closeParen,
            ToKeyword = toKeyword,
            ToOpen = toOpen,
            ToValues = toValues,
            ToClose = toClose,
        };
    }
}
