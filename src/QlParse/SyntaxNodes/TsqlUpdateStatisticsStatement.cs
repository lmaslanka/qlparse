namespace QlParse;

// `UPDATE STATISTICS table [name | (name, ...)] [WITH FULLSCAN | SAMPLE n PERCENT | RESAMPLE, ...]`.
public sealed class TsqlUpdateStatisticsStatement : Query
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required SyntaxToken StatisticsKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Statistics { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
