namespace QlParse;

public sealed class OffsetClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OffsetKeyword { get; init; }
    public required Expression Count { get; init; }
    public SyntaxToken? RowKeyword { get; init; }
}
