namespace QlParse;

public sealed class OrderByItem
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullOrder { get; init; }
}
