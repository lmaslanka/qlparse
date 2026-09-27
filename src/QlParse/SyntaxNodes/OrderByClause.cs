namespace QlParse;

public sealed class OrderByClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<OrderByItem> Items { get; init; }
}
