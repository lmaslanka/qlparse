namespace QlParse;

public sealed class CollateExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken CollateKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
