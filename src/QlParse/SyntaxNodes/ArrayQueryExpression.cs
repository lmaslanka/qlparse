namespace QlParse;

public sealed class ArrayQueryExpression : Expression
{
    public required SyntaxToken ArrayKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
