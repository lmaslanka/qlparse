namespace QlParse;

// Postgres-only parts of CREATE TABLE (UNLOGGED, IF NOT EXISTS, PARTITION OF / BY, INHERITS, USING,
// WITH (storage parameters), WITHOUT OIDS, TABLESPACE);
// allocated only when at least one is present.
public sealed class PgCreateTableExtras
{
    public SyntaxToken? UnloggedKeyword { get; init; }
    public ExistenceCheck? IfNotExists { get; init; }
    public PartitionOfClause? PartitionOf { get; init; }
    public SyntaxToken? InheritsKeyword { get; init; }
    public SyntaxToken? InheritsOpen { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Inherits { get; init; }
    public SyntaxToken? InheritsClose { get; init; }
    public PartitionByClause? PartitionBy { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? AccessMethod { get; init; }
    public SyntaxToken? StorageWithKeyword { get; init; }
    public StorageParameterList? StorageParameters { get; init; }
    public SyntaxToken? WithoutKeyword { get; init; }
    public SyntaxToken? OidsKeyword { get; init; }
    public SyntaxToken? TablespaceKeyword { get; init; }
    public SyntaxToken? Tablespace { get; init; }
}
