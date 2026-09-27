namespace QlParse;

public sealed class CollectionSuffix
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Keyword { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public SyntaxToken? Cardinality { get; init; }
    public SyntaxToken? CloseBracket { get; init; }
    public IReadOnlyList<MdarrayDimension>? Dimensions { get; init; }
}
