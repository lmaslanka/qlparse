namespace QlParse;

public sealed class BinaryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}
