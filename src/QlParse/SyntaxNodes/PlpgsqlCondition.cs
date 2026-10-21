namespace QlParse;

public sealed class PlpgsqlCondition
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
