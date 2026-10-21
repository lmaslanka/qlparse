namespace QlParse;

public sealed class ConflictTargetElement
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public IReadOnlyList<SyntaxToken> OperatorClass { get; init; } = [];
}
