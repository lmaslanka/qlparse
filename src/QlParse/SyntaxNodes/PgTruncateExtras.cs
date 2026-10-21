namespace QlParse;

// Postgres `TRUNCATE [ONLY] name [*] [, ...] ... [CASCADE | RESTRICT]`: the first table's ONLY / `*`,
// every further table, and the drop behavior.
public sealed class PgTruncateExtras
{
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? DescendantStar { get; init; }
    public IReadOnlyList<LockTarget> AdditionalTables { get; init; } = [];
    public SyntaxToken? Behavior { get; init; }
}
