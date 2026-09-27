namespace QlParse;

public abstract class Query
{
    public required SourceSpan Span { get; init; }
}
