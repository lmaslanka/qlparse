namespace QlParse;

public sealed class GraphTableElement
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> KeyColumns { get; init; } = [];
    public SyntaxToken? SourceKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> SourceColumns { get; init; } = [];
    public IReadOnlyList<SyntaxToken> SourceReference { get; init; } = [];
    public SyntaxToken? DestinationKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> DestinationColumns { get; init; } = [];
    public IReadOnlyList<SyntaxToken> DestinationReference { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Labels { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Properties { get; init; } = [];
}
