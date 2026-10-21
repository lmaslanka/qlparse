namespace QlParse;

// Postgres `AS [NOT] MATERIALIZED` on a common table expression.
public sealed class PgCteExtras
{
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken MaterializedKeyword { get; init; }
}
