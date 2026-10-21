namespace QlParse;

// SQL Server MERGE: `MERGE [TOP (n) [PERCENT]] [INTO] target [WITH (hints)] [[AS] alias] USING source ON condition
// WHEN ... [OUTPUT ...] [OPTION (...)]`. WHEN clauses reuse MergeWhenClause with T-SQL actions.
public sealed class TsqlMergeStatement : Query
{
    public required SyntaxToken MergeKeyword { get; init; }
    public TsqlTopClause? Top { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public required TableSource Target { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required TableSource Source { get; init; }
    public IReadOnlyList<JoinClause> SourceJoins { get; init; } = [];
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required IReadOnlyList<MergeWhenClause> Whens { get; init; }
    public IReadOnlyList<TsqlOutputClause> Output { get; init; } = [];
    public TsqlOptionClause? Option { get; init; }
}
