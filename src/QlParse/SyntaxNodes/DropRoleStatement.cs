namespace QlParse;

public sealed class DropRoleStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
