namespace QlParse;

public sealed class SelectStatement : Query
{
    public required SyntaxToken SelectKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public required IReadOnlyList<SelectItem> SelectList { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? IntoTargets { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public GroupByClause? GroupBy { get; init; }
    public HavingClause? Having { get; init; }
    public WindowClause? Window { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public LimitClause? Limit { get; init; }
    public OffsetClause? Offset { get; init; }
    public FetchClause? Fetch { get; init; }
    public LockClause? Lock { get; init; }
    public PgSelectExtras? Postgres { get => _dialect as PgSelectExtras; init => _dialect ??= value; }
    public TsqlSelectExtras? SqlServer { get => _dialect as TsqlSelectExtras; init => _dialect ??= value; }

    // The Postgres and SQL Server extras share one slot (a statement is parsed in one dialect), so ISO
    // selects keep their size.
    private readonly object? _dialect;
}
