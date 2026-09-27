namespace QlParse;

public sealed class ScalarSubqueryExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
