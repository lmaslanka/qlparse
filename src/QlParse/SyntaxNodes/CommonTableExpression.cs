namespace QlParse;

public sealed class CommonTableExpression
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken OpenQuery { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseQuery { get; init; }
    public SearchClause? Search { get; init; }
    public CycleClause? Cycle { get; init; }
}
