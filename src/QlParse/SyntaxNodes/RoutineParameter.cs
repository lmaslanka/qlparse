namespace QlParse;

public sealed class RoutineParameter
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Mode { get; init; }
    public SyntaxToken? Name { get; init; }
    public required DataType Type { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? LocatorKeyword { get; init; }
    public SyntaxToken? ResultKeyword { get; init; }
}
