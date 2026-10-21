namespace QlParse;

// Postgres-only parts shared by DROP TABLE, DROP SEQUENCE and DROP SCHEMA (IF EXISTS, further names);
// allocated only when at least one is present.
public sealed class PgDropExtras
{
    public ExistenceCheck? IfExists { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames { get; init; } = [];
}
