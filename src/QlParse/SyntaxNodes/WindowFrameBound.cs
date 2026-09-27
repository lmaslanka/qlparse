namespace QlParse;

public sealed class WindowFrameBound
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? UnboundedKeyword { get; init; }
    public SyntaxToken? CurrentKeyword { get; init; }
    public Expression? Offset { get; init; }
    public required SyntaxToken Endpoint { get; init; }
}
