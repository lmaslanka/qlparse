namespace QlParse;

public sealed class IntervalQualifier
{
    public required SourceSpan Span { get; init; }
    public required IntervalField Start { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IntervalField? End { get; init; }
}
