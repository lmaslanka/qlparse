namespace QlParse;

public sealed class AlterTableStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterTableAction Action { get; init; }

    // Postgres and SQL Server extras share one slot (no node carries both), so the T-SQL holder adds nothing
    // to ISO instances.
    private readonly object? _dialect;

    public PgAlterTableExtras? Postgres { get => _dialect as PgAlterTableExtras; init => _dialect ??= value; }

    public TsqlAlterTableExtras? SqlServer { get => _dialect as TsqlAlterTableExtras; init => _dialect ??= value; }
}
