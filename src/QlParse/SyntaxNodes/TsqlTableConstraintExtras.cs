namespace QlParse;

// SQL Server-only parts of a table or column constraint (CLUSTERED / NONCLUSTERED [HASH], per-column ASC/DESC,
// WITH (index options), ON storage, NOT FOR REPLICATION, `DEFAULT expr FOR column [WITH VALUES]`, edge
// CONNECTION (...)); allocated only when at least one is present. ColumnDirections parallels Columns.
public sealed class TsqlTableConstraintExtras
{
    public SyntaxToken? ClusteredKeyword { get; init; }
    public SyntaxToken? HashKeyword { get; init; }
    public IReadOnlyList<SyntaxToken?>? ColumnDirections { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
    public TsqlStorageClause? Storage { get; init; }
    public TsqlNotForReplication? NotForReplication { get; init; }
    public DefaultClause? Default { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? ForColumn { get; init; }
    public SyntaxToken? ValuesWithKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public TsqlEdgeConnectionClause? Connection { get; init; }
}
