namespace QlParse;

public sealed class PgOptionItem
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Action { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Value { get; init; }
}
