namespace QlParse;

// Postgres `ALTER SCHEMA name OWNER TO role`;
// allocated only when at least one is present.
public sealed class PgAlterSchemaExtras
{
    public SyntaxToken? OwnerKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
}
