namespace QlParse;

public sealed class PartitionOfClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Parent { get; init; }
    public required PartitionBound Bound { get; init; }
}
