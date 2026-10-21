namespace QlParse;

public sealed class PlpgsqlExceptionHandler
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public IReadOnlyList<PlpgsqlCondition> Conditions { get; init; } = [];
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
}
