namespace QlParse;

public sealed class HavingClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken HavingKeyword { get; init; }
    public required Expression Expression { get; init; }
}
