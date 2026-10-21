namespace QlParse;

// Postgres-only parts of DROP INDEX (CONCURRENTLY, IF EXISTS, further names);
// allocated only when at least one is present.
public sealed class PgDropIndexExtras
{
    public SyntaxToken? ConcurrentlyKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames { get; init; } = [];
}
