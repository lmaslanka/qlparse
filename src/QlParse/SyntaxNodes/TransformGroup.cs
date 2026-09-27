namespace QlParse;

public sealed class TransformGroup
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TransformElement> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
