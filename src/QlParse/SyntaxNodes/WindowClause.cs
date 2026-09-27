namespace QlParse;

public sealed class WindowClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WindowKeyword { get; init; }
    public required IReadOnlyList<WindowDefinition> Windows { get; init; }
}
