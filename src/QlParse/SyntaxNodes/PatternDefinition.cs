namespace QlParse;

public sealed class PatternDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Expression Condition { get; init; }
}
