namespace QlParse;

public sealed class PositionedClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required SyntaxToken CurrentKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}
