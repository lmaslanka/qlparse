namespace QlParse;

// `OPTION (query_hint, ...)` at the end of a SELECT or DML statement.
public sealed class TsqlOptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OptionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlHint> Hints { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
