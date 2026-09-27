namespace QlParse;

public sealed class DeclareConditionStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken ConditionKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
}
