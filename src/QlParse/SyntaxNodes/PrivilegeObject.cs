namespace QlParse;

public sealed class PrivilegeObject
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Kind { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public RoutineDesignator? Routine { get; init; }
}
