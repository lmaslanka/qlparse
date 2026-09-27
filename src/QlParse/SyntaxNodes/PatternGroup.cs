namespace QlParse;

public sealed class PatternGroup : RowPattern
{
    public required SyntaxToken OpenParen { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
