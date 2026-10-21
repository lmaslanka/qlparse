namespace QlParse;

public sealed class DistinctOnClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Expressions { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
