namespace QlParse;

// A partition number or `start TO end` range in TRUNCATE ... WITH (PARTITIONS (...)).
public sealed class TsqlTruncatePartitionRange
{
    public required SourceSpan Span { get; init; }
    public required Expression Start { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public Expression? End { get; init; }
}
