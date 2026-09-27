namespace QlParse;

public sealed class SetRoleStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
