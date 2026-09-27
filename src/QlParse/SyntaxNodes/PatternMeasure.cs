namespace QlParse;

public sealed class PatternMeasure
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Semantics { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
