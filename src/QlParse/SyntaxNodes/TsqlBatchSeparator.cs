namespace QlParse;

// `GO [count]` on a line of its own: the client-side batch separator and its optional repeat count.
public sealed class TsqlBatchSeparator
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GoKeyword { get; init; }
    public SyntaxToken? Count { get; init; }
}
