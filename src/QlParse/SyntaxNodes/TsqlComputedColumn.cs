namespace QlParse;

// `AS expression [PERSISTED]` computed column body.
public sealed class TsqlComputedColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? PersistedKeyword { get; init; }
}
