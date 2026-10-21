namespace QlParse;

// Postgres-only parts of ALTER TABLE (IF EXISTS, ONLY / `*`, ALL IN TABLESPACE, further actions);
// allocated only when at least one is present.
public sealed class PgAlterTableExtras
{
    public ExistenceCheck? IfExists { get; init; }
    public AllInTablespaceClause? AllInTablespace { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? Star { get; init; }
    public IReadOnlyList<AlterTableAction> AdditionalActions { get; init; } = [];
}
