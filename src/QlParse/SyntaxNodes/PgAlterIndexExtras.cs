namespace QlParse;

// Postgres-only parts of ALTER INDEX (IF EXISTS, relation actions, ALL IN TABLESPACE);
// allocated only when at least one is present.
public sealed class PgAlterIndexExtras
{
    public ExistenceCheck? IfExists { get; init; }
    public AlterTableAction? RelationAction { get; init; }

    // Postgres `ALTER INDEX ALL IN TABLESPACE ...` (Name is then empty).
    public AllInTablespaceClause? AllInTablespace { get; init; }
}
