namespace QlParse;

public sealed class SequenceOption
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public Expression? Value { get; init; }
}
