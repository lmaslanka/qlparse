namespace QlParse;

public sealed class UnaryExpression : Expression
{
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Expression { get; init; }
}
