namespace QlParse;

public sealed class PgHandlerClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken Keyword { get; init; }
    public IReadOnlyList<SyntaxToken> Function { get; init; } = [];
}
