namespace QlParse;

// `CREATE STATISTICS name ON table (columns) [WHERE filter] [WITH FULLSCAN | SAMPLE n {PERCENT|ROWS}, ...]`.
public sealed class TsqlCreateStatisticsStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken StatisticsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
