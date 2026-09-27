namespace QlParse;

public sealed class SelectItem
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
