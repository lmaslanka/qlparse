namespace QlParse;

public sealed class CreateIndexStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? ClusteredKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<IndexColumn> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }

    // Postgres and SQL Server extras share one slot (no node carries both), so the T-SQL holder adds nothing
    // to ISO instances.
    private readonly object? _dialect;

    public PgCreateIndexExtras? Postgres { get => _dialect as PgCreateIndexExtras; init => _dialect ??= value; }

    public TsqlCreateIndexExtras? SqlServer { get => _dialect as TsqlCreateIndexExtras; init => _dialect ??= value; }
}
