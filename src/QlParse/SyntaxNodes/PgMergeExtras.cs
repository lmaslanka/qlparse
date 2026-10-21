namespace QlParse;

// Postgres-only parts of a MERGE; allocated only when at least one is present.
public sealed class PgMergeExtras
{
    public SyntaxToken? TargetDescendantStar { get; init; }
    public ReturningClause? Returning { get; init; }
}
