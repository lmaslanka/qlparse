namespace QlParse;

public sealed class PeriodPredicateExpression : Expression
{
    public required Expression Left { get; init; }
    public SyntaxToken? ImmediatelyKeyword { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}
