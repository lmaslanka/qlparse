namespace QlParse;

// SQL Server join hint (`INNER HASH JOIN`, `LEFT MERGE JOIN`, `LOOP`, `REMOTE`). It carries the join's constraint so
// both share one slot of JoinClause; JoinClause.Constraint reads it from here.
public sealed class TsqlJoinExtras
{
    public required SyntaxToken Hint { get; init; }
    public JoinConstraint? Constraint { get; init; }
}
