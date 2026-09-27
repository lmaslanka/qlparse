namespace QlParse;

public sealed class CommaFrom
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
}
