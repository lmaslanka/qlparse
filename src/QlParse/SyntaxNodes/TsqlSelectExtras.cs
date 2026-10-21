namespace QlParse;

// SQL Server-only parts of a SELECT; allocated only when at least one is present. SELECT ... INTO keeps its
// INTO keyword on the statement and the new table here.
public sealed class TsqlSelectExtras
{
    public TsqlTopClause? Top { get; init; }
    public SelectIntoTable? IntoTable { get; init; }
    public SyntaxToken? IntoOnKeyword { get; init; }
    public SyntaxToken? IntoFilegroup { get; init; }
    public SyntaxToken? GroupByAllKeyword { get; init; }
    public SyntaxToken? GroupByWithKeyword { get; init; }
    public SyntaxToken? GroupByWithOption { get; init; }
    public IReadOnlyList<TsqlComputeClause> Compute { get; init; } = [];
    public TsqlForClause? For { get; init; }
    public TsqlOptionClause? Option { get; init; }
}
