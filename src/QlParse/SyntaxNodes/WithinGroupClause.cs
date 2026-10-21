namespace QlParse;

public sealed class WithinGroupClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WithinKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required OrderByClause OrderBy { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
