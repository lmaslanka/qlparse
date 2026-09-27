namespace QlParse;

public sealed class IntervalField
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
