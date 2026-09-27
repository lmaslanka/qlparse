namespace QlParse;

public sealed class FetchClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken FetchKeyword { get; init; }
    public required SyntaxToken PositionKeyword { get; init; }
    public Expression? Count { get; init; }
    public SyntaxToken? PercentKeyword { get; init; }
    public required SyntaxToken RowKeyword { get; init; }
    public required SyntaxToken OnlyOrWith { get; init; }
    public SyntaxToken? TiesKeyword { get; init; }
}
