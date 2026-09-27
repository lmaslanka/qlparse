namespace QlParse;

public sealed class RoutineDesignator
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? SpecificKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public required SyntaxToken RoutineType { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ForType { get; init; }
}
