namespace QlParse;

public sealed class PartitionByClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required SyntaxToken Strategy { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<IndexColumn> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
