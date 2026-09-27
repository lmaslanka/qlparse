namespace QlParse;

public sealed class CaseStatementWhen
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public required Expression Operand { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
}
