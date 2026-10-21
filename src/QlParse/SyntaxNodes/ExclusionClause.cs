namespace QlParse;

public sealed class ExclusionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ExcludeKeyword { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? Method { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<ExclusionElement> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IndexParameters? IndexParameters { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public SyntaxToken? WhereOpen { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? WhereClose { get; init; }
}
