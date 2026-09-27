namespace QlParse;

public sealed class MultisetQueryExpression : Expression
{
    public required SyntaxToken Keyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
