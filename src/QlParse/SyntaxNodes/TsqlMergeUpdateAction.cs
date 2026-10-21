namespace QlParse;

// `WHEN ... THEN UPDATE SET ...` with T-SQL SET items.
public sealed class TsqlMergeUpdateAction : MergeAction
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<TsqlSetClause> Assignments { get; init; }
}
