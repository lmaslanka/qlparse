namespace QlParse;

public sealed class OptionValue
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Sign { get; init; }
    public required SyntaxToken Value { get; init; }
}
