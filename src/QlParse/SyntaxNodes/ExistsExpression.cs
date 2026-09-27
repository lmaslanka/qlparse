namespace QlParse;

public sealed class ExistsExpression : Expression
{
    public required SyntaxToken ExistsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
