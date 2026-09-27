namespace QlParse;

public sealed class PtfTable : TableSource
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<PtfArgument> Arguments { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
