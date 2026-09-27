namespace QlParse;

public sealed class WindowFrame
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Units { get; init; }
    public SyntaxToken? BetweenKeyword { get; init; }
    public required WindowFrameBound Start { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public WindowFrameBound? End { get; init; }
    public SyntaxToken? ExcludeKeyword { get; init; }
    public SyntaxToken? Exclusion { get; init; }
    public SyntaxToken? ExclusionTail { get; init; }
}
