namespace QlParse;

public sealed class MdarrayAggregateExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Argument { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
