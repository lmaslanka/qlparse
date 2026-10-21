namespace QlParse;

public sealed class AlterPolicyStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken PolicyKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? RenameKeyword { get; init; }
    public SyntaxToken? RenameToKeyword { get; init; }
    public SyntaxToken? NewName { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Roles { get; init; } = [];
    public PolicyCondition? Using { get; init; }
    public PolicyCondition? WithCheck { get; init; }
}
