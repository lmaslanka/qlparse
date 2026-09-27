namespace QlParse;

public sealed class CastExpression : Expression
{
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
