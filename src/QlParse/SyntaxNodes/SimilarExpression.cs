namespace QlParse;

public sealed class SimilarExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken SimilarKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression Pattern { get; init; }
    public SyntaxToken? EscapeKeyword { get; init; }
    public Expression? Escape { get; init; }
}
