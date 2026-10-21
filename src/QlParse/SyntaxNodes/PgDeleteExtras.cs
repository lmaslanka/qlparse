namespace QlParse;

// Postgres-only parts of a DELETE; allocated only when at least one is present.
public sealed class PgDeleteExtras
{
    public SyntaxToken? TargetDescendantStar { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public TableSource? Using { get; init; }
    public IReadOnlyList<JoinClause> UsingJoins { get; init; } = [];
    public IReadOnlyList<CommaFrom> ExtraUsing { get; init; } = [];
    public ReturningClause? Returning { get; init; }
}
