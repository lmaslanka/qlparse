namespace QlParse;

// Postgres-only parts of an UPDATE; allocated only when at least one is present.
public sealed class PgUpdateExtras
{
    public SyntaxToken? TargetDescendantStar { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public IReadOnlyList<JoinClause> Joins { get; init; } = [];
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public ReturningClause? Returning { get; init; }
}
