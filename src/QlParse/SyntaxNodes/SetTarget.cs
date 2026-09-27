namespace QlParse;

public sealed class SetTarget
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public Expression? Index { get; init; }
    public SyntaxToken? CloseBracket { get; init; }
}
