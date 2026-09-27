namespace QlParse;

public sealed class PatternExclusion : RowPattern
{
    public required SyntaxToken OpenBrace { get; init; }
    public required SyntaxToken Minus { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken CloseMinus { get; init; }
    public required SyntaxToken CloseBrace { get; init; }
}
