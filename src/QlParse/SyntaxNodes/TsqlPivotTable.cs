namespace QlParse;

// `table PIVOT (aggregate FOR column IN ([a], [b], ...)) [AS] alias`.
public sealed class TsqlPivotTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken PivotKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Aggregate { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required Expression Column { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken ValuesOpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Values { get; init; }
    public required SyntaxToken ValuesCloseParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
}
