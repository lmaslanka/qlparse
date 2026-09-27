namespace QlParse;

public sealed class GraphProperty
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Colon { get; init; }
    public required Expression Value { get; init; }
}
