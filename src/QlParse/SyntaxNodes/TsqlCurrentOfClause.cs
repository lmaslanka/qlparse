namespace QlParse;

// `WHERE CURRENT OF [GLOBAL] cursor | @cursor_variable`.
public sealed class TsqlCurrentOfClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required SyntaxToken CurrentKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public SyntaxToken? GlobalKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}
