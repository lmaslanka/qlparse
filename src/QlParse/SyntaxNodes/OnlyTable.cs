namespace QlParse;

public sealed class OnlyTable : TableSource
{
    public required SyntaxToken OnlyKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public SystemTimeClause? SystemTime { get; init; }
}
