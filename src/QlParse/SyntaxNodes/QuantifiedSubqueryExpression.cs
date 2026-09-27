namespace QlParse;

public sealed class QuantifiedSubqueryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required SyntaxToken Quantifier { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
