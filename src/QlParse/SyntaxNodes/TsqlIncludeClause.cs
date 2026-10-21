namespace QlParse;

// `INCLUDE (column, ...)` non-key columns of an index.
public sealed class TsqlIncludeClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken IncludeKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
