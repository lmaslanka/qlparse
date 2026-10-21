namespace QlParse;

// `DBCC command [(argument, ...)] [WITH option, ...]`.
public sealed class TsqlDbccStatement : Query
{
    public required SyntaxToken DbccKeyword { get; init; }
    public required SyntaxToken Command { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
