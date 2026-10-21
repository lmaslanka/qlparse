namespace QlParse;

public sealed class LimitClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken LimitKeyword { get; init; }
    public Expression? Count { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
}
