namespace QlParse;

public sealed class ExclusionElement
{
    public required SourceSpan Span { get; init; }
    public required IndexColumn Column { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Operator { get; init; }
}
