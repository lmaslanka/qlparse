namespace QlParse;

public sealed class CreatePolicyStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PolicyKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? PolicyType { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? Command { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Roles { get; init; } = [];
    public PolicyCondition? Using { get; init; }
    public PolicyCondition? WithCheck { get; init; }
}
