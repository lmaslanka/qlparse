namespace QlParse;

// SQL Server DELETE: `DELETE [TOP (n) [PERCENT]] [FROM] target [WITH (hints)] [OUTPUT ...] [FROM ...]
// [WHERE ... | WHERE CURRENT OF ...] [OPTION (...)]`. SourceFromKeyword is the second FROM.
public sealed class TsqlDeleteStatement : Query
{
    public required SyntaxToken DeleteKeyword { get; init; }
    public TsqlTopClause? Top { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required TableSource Target { get; init; }
    public IReadOnlyList<TsqlOutputClause> Output { get; init; } = [];
    public SyntaxToken? SourceFromKeyword { get; init; }
    public TableSource? From { get; init; }
    public IReadOnlyList<JoinClause> Joins { get; init; } = [];
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public TsqlCurrentOfClause? CurrentOf { get; init; }
    public TsqlOptionClause? Option { get; init; }
}
