namespace QlParse;

// Postgres-only parts of DROP ROLE (IF EXISTS, the full name list);
// allocated only when at least one is present.
public sealed class PgDropRoleExtras
{
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Names { get; init; } = [];
}
