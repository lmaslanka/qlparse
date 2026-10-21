namespace QlParse;

public sealed class AlterDefaultPrivilegesStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required SyntaxToken PrivilegesKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? RoleKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> TargetRoles { get; init; } = [];
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Schemas { get; init; } = [];
    public required Query Action { get; init; }
}
