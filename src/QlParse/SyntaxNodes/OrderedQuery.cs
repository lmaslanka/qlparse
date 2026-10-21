namespace QlParse;

// Postgres ORDER BY / LIMIT / OFFSET / FETCH / locking clauses after a query that does not end in a
// SELECT: `TABLE t ORDER BY a`, `VALUES (1), (2) LIMIT 1`, `TABLE t UNION TABLE u ORDER BY 1`.
// A trailing SELECT keeps these clauses itself, exactly as under ISO.
public sealed class OrderedQuery : Query
{
    public required Query Query { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public LimitClause? Limit { get; init; }
    public OffsetClause? Offset { get; init; }
    public FetchClause? Fetch { get; init; }
    public LockClause? Lock { get; init; }
}
