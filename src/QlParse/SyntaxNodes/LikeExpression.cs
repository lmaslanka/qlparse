namespace QlParse;

public sealed class LikeExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken LikeKeyword { get; init; }
    public required Expression Pattern { get; init; }
    public SyntaxToken? EscapeKeyword { get; init; }
    public Expression? Escape { get; init; }
}
