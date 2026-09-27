namespace QlParse;

public sealed class IndexColumn
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullsOrder { get; init; }
}
