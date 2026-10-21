namespace QlParse;

// SQL Server `TRUNCATE TABLE t WITH (PARTITIONS (1, 3 TO 5))`.
public sealed class TsqlTruncateExtras
{
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken PartitionsKeyword { get; init; }
    public required SyntaxToken PartitionsOpenParen { get; init; }
    public required IReadOnlyList<TsqlTruncatePartitionRange> Partitions { get; init; }
    public required SyntaxToken PartitionsCloseParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
