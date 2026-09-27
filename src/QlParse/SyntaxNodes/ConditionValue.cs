namespace QlParse;

public sealed class ConditionValue
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
