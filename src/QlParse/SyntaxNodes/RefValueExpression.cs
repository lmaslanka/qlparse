namespace QlParse;

public sealed class RefValueExpression : Expression
{
    public required SyntaxToken RefKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
