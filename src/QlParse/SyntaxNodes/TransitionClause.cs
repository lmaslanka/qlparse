namespace QlParse;

public sealed class TransitionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public SyntaxToken? RowOrTable { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
