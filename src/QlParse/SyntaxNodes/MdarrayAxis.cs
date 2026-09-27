namespace QlParse;

public sealed class MdarrayAxis
{
    public required SourceSpan Span { get; init; }
    public Expression? Lower { get; init; }
    public SyntaxToken? Colon { get; init; }
    public Expression? Upper { get; init; }
    public SyntaxToken? Star { get; init; }
}
