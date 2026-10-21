namespace QlParse;

// `TOP (expression) [PERCENT] [WITH TIES]`, or the legacy `TOP n` without parentheses (SELECT only).
public sealed class TsqlTopClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken TopKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required Expression Count { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? PercentKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? TiesKeyword { get; init; }
}
