namespace QlParse;

// Postgres-only parts of SET TIME ZONE (SESSION / LOCAL scope, DEFAULT);
// allocated only when at least one is present.
public sealed class PgSetTimeZoneExtras
{
    public SyntaxToken? ScopeKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
}
