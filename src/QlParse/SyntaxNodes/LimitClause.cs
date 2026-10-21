namespace QlParse;

public sealed class LimitClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken LimitKeyword { get; init; }

    // Null only for Postgres `LIMIT ALL` (see Postgres.AllKeyword).
    public Expression? Count { get; init; }
    public PgLimitExtras? Postgres { get; init; }
}
