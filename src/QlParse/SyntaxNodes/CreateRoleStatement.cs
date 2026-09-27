namespace QlParse;

public sealed class CreateRoleStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? AdminKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}
