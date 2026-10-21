namespace QlParse;

public sealed class ReturningOutputAlias
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
}
