namespace QlParse;

public sealed class DiagnosticsItem
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Target { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required SyntaxToken Name { get; init; }
}
