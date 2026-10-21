namespace QlParse;

// SQL Server UPDATE: `UPDATE [TOP (n) [PERCENT]] target [WITH (hints)] SET ... [OUTPUT ...] [FROM ...]
// [WHERE ... | WHERE CURRENT OF ...] [OPTION (...)]`.
public sealed class TsqlUpdateStatement : Query
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public TsqlTopClause? Top { get; init; }
    public required TableSource Target { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<TsqlSetClause> Assignments { get; init; }
    public IReadOnlyList<TsqlOutputClause> Output { get; init; } = [];
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public IReadOnlyList<JoinClause> Joins { get; init; } = [];
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public TsqlCurrentOfClause? CurrentOf { get; init; }
    public TsqlOptionClause? Option { get; init; }
}
