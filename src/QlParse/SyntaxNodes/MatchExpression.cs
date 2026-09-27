namespace QlParse;

public sealed class MatchExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken MatchKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? MatchType { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
