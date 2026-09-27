namespace QlParse;

public sealed class ReferentialAction
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public required SyntaxToken Action { get; init; }
}
