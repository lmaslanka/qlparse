namespace QlParse;

public sealed class PgEventTriggerFilter
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Values { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
