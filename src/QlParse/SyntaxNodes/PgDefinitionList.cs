namespace QlParse;

public sealed class PgDefinitionList
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<PgDefinitionElement> Elements { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
