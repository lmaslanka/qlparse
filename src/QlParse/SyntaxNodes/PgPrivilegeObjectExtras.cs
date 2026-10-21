namespace QlParse;

// Postgres-only parts of a GRANT / REVOKE object (multi-word kinds, ALL ... IN SCHEMA, every target);
// allocated only when at least one is present.
public sealed class PgPrivilegeObjectExtras
{
    public IReadOnlyList<SyntaxToken> ExtraKindKeywords { get; init; } = [];
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public IReadOnlyList<ObjectSignature> Targets { get; init; } = [];
}
