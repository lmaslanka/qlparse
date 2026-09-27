namespace QlParse;

public sealed class ElseIfClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ElseIfKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
}
