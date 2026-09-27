namespace QlParse;

public sealed class GraphElement
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Variable { get; init; }
    public IReadOnlyList<SyntaxToken> Labels { get; init; } = [];
    public IReadOnlyList<GraphProperty> Properties { get; init; } = [];
    public Expression? Where { get; init; }
    public bool Edge { get; init; }
    public SyntaxToken? Quantifier { get; init; }
    public IReadOnlyList<GraphElement> Group { get; init; } = [];
}
