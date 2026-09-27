namespace QlParse;

public sealed class OverlapsExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OverlapsKeyword { get; init; }
    public required Expression Right { get; init; }
}
