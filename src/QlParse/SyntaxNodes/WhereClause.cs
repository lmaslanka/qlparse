namespace QlParse;

public sealed class WhereClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
}
