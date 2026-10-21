namespace QlParse;

public sealed class OrdinalityClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken OrdinalityKeyword { get; init; }
}
