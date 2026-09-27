namespace QlParse;

public sealed class RevokePrivilegeStatement : Query
{
    public required SyntaxToken RevokeKeyword { get; init; }
    public RevokeOptionClause? Option { get; init; }
    public required IReadOnlyList<PrivilegeAction> Actions { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required PrivilegeObject Object { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
