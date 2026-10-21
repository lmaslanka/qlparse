namespace QlParse;

public sealed class PgTriggerEvent
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OrKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
}
