namespace QlParse;

// `table UNPIVOT (value_column FOR name_column IN ([a], [b], ...)) [AS] alias`.
public sealed class TsqlUnpivotTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken UnpivotKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken ValueColumn { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken NameColumn { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken ColumnsOpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken ColumnsCloseParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
}
