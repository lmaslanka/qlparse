namespace QlParse;

public abstract class Expression
{
    public required SourceSpan Span { get; init; }
}
