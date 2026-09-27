namespace QlParse;

public sealed class PortionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken PortionKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Start { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression End { get; init; }
}
