namespace QlParse;

public sealed class LockTarget
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Star { get; init; }
}
