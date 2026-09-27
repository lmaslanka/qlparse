namespace QlParse;

public sealed class BetweenExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken BetweenKeyword { get; init; }
    public required Expression Lower { get; init; }
    public required SyntaxToken AndKeyword { get; init; }
    public required Expression Upper { get; init; }
}
