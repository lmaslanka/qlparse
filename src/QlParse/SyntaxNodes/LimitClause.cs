namespace QlParse;

public sealed class LimitClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken LimitKeyword { get; init; }
    public required Expression Count { get; init; }
}
