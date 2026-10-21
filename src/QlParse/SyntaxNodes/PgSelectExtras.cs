namespace QlParse;

// Postgres-only clauses of a SELECT; allocated only when at least one is present.
public sealed class PgSelectExtras
{
    public DistinctOnClause? DistinctOn { get; init; }
    public SelectIntoTable? IntoTable { get; init; }
}
