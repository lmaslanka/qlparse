namespace QlParse;

public sealed class PatternPermute : RowPattern
{
    public required SyntaxToken PermuteKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<RowPattern> Patterns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
