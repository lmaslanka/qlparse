namespace QlParse;

public sealed class WhenClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public required Expression Result { get; init; }
}
