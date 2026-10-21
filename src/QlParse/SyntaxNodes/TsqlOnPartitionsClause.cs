namespace QlParse;

// `ON PARTITIONS (1, 3 TO 5)` after a DATA_COMPRESSION / XML_COMPRESSION option.
public sealed class TsqlOnPartitionsClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken PartitionsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlPartitionNumberRange> Ranges { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
