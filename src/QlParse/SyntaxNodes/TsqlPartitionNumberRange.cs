namespace QlParse;

// A partition number or an inclusive `start TO end` range.
public sealed class TsqlPartitionNumberRange
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Start { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? End { get; init; }
}
