namespace QlParse;

public sealed class WindowDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required WindowSpecification Specification { get; init; }
}
