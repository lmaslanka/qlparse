namespace QlParse;

public sealed class PatternSubset
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Variables { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
