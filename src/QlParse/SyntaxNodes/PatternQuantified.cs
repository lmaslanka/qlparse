namespace QlParse;

public sealed class PatternQuantified : RowPattern
{
    public required RowPattern Primary { get; init; }
    public required SyntaxToken Quantifier { get; init; }
    public SyntaxToken? Low { get; init; }
    public SyntaxToken? Comma { get; init; }
    public SyntaxToken? High { get; init; }
    public SyntaxToken? CloseBrace { get; init; }
    public SyntaxToken? Reluctant { get; init; }
}
