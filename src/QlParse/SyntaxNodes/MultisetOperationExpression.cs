namespace QlParse;

public sealed class MultisetOperationExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken MultisetKeyword { get; init; }
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? Quantifier { get; init; }
    public required Expression Right { get; init; }
}
