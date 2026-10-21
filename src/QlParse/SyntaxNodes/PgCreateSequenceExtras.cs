namespace QlParse;

// Postgres-only parts of CREATE SEQUENCE (TEMPORARY / UNLOGGED, IF NOT EXISTS);
// allocated only when at least one is present.
public sealed class PgCreateSequenceExtras
{
    public SyntaxToken? PersistenceKeyword { get; init; }
    public ExistenceCheck? IfNotExists { get; init; }
}
