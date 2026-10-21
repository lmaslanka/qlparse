namespace QlParse;

public sealed class RowsFromTable : TableSource
{
    public required SyntaxToken RowsKeyword { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<RowsFromFunction> Functions { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public OrdinalityClause? Ordinality { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
