namespace QlParse;

public sealed class FilterClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken FilterKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
