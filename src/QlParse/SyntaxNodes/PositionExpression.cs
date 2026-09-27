namespace QlParse;

public sealed class PositionExpression : Expression
{
    public required SyntaxToken PositionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Needle { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required Expression Haystack { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
