namespace QlParse;

public sealed class AlterRoleStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? RenameKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? NewName { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? DatabaseKeyword { get; init; }
    public SyntaxToken? Database { get; init; }
    public Query? Setting { get; init; }
}
