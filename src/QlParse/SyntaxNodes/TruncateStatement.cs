namespace QlParse;

public sealed class TruncateStatement : Query
{
    public required SyntaxToken TruncateKeyword { get; init; }

    // Null only for Postgres, where TABLE is optional.
    public SyntaxToken? TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? RestartKeyword { get; init; }
    public SyntaxToken? IdentityKeyword { get; init; }
    public PgTruncateExtras? Postgres { get => _dialect as PgTruncateExtras; init => _dialect ??= value; }
    public TsqlTruncateExtras? SqlServer { get => _dialect as TsqlTruncateExtras; init => _dialect ??= value; }

    // The Postgres and SQL Server extras share one slot (a statement is parsed in one dialect).
    private readonly object? _dialect;
}
