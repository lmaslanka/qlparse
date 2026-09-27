namespace QlParse;

public sealed class TableReference : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public SystemTimeClause? SystemTime { get; init; }
}
