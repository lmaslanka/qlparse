namespace QlParse;

public sealed class TransformElement
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Direction { get; init; }
    public required SyntaxToken SqlKeyword { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required RoutineDesignator Routine { get; init; }
}
