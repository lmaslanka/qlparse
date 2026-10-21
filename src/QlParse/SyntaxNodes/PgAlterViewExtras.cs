namespace QlParse;

// Postgres-only parts of ALTER [MATERIALIZED] VIEW (MATERIALIZED, IF EXISTS, ALL IN TABLESPACE);
// allocated only when at least one is present.
public sealed class PgAlterViewExtras
{
    public SyntaxToken? MaterializedKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }

    // Postgres `ALTER MATERIALIZED VIEW ALL IN TABLESPACE ...` (Name is then empty).
    public AllInTablespaceClause? AllInTablespace { get; init; }
}
