namespace QlParse;

public sealed class SearchClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken SearchKeyword { get; init; }
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken FirstKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SequenceColumn { get; init; }
}
