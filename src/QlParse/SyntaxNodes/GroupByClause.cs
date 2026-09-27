namespace QlParse;

public sealed class GroupByClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<Expression> Keys { get; init; }
}
