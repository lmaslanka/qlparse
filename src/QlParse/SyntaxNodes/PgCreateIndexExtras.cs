namespace QlParse;

// Postgres-only parts of CREATE INDEX (CONCURRENTLY, IF NOT EXISTS, ONLY, USING method, INCLUDE / WITH / TABLESPACE);
// allocated only when at least one is present.
public sealed class PgCreateIndexExtras
{
    public SyntaxToken? ConcurrentlyKeyword { get; init; }
    public ExistenceCheck? IfNotExists { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? Method { get; init; }
    public IndexParameters? Options { get; init; }
}
