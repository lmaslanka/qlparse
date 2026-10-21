namespace QlParse;

public sealed class DropTriggerStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
