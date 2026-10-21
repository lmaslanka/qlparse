namespace QlParse;

// Postgres `LIMIT ALL`.
public sealed class PgLimitExtras
{
    public required SyntaxToken AllKeyword { get; init; }
}
