namespace QlParse;

// Postgres `TRUNCATE ONLY name` / `TRUNCATE name *`.
public sealed class PgTruncateExtras
{
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? DescendantStar { get; init; }
}
