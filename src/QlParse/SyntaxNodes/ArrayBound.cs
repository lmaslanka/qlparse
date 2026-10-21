namespace QlParse;

public sealed class ArrayBound
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public SyntaxToken? Size { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}
