namespace QlParse;

// Postgres-only parts of an INSERT; allocated only when at least one is present.
public sealed class PgInsertExtras
{
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public OnConflictClause? OnConflict { get; init; }
    public ReturningClause? Returning { get; init; }
}
