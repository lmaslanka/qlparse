namespace QlParse;

public sealed class GrantPrivilegeStatement : Query
{
    public required SyntaxToken GrantKeyword { get; init; }
    public required IReadOnlyList<PrivilegeAction> Actions { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required PrivilegeObject Object { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public GrantOptionClause? HierarchyOption { get; init; }
    public GrantOptionClause? GrantOption { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}
