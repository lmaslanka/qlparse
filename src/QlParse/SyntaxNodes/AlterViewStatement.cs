namespace QlParse;

public sealed class AlterViewStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterViewAction Action { get; init; }
    public SyntaxToken? MaterializedKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }

    // Postgres `ALTER MATERIALIZED VIEW ALL IN TABLESPACE ...` (Name is then empty).
    public AllInTablespaceClause? AllInTablespace { get; init; }
}
