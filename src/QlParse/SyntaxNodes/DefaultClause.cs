namespace QlParse;

public sealed class DefaultClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required Expression Value { get; init; }
}
