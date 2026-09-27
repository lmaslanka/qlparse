namespace QlParse;

public sealed class MdarrayDimension
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Lower { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken Upper { get; init; }
}
