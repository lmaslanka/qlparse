namespace QlParse;

// `table TABLESAMPLE [SYSTEM] (size [PERCENT | ROWS]) [REPEATABLE (seed)]`.
public sealed class TsqlSampledTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken TablesampleKeyword { get; init; }
    public SyntaxToken? SystemKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Size { get; init; }
    public SyntaxToken? Unit { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? RepeatableKeyword { get; init; }
    public SyntaxToken? RepeatOpenParen { get; init; }
    public Expression? Seed { get; init; }
    public SyntaxToken? RepeatCloseParen { get; init; }
}
