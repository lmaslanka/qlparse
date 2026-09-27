namespace QlParse;

public sealed class GrantRoleStatement : Query
{
    public required SyntaxToken GrantKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Roles { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public GrantOptionClause? AdminOption { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}
