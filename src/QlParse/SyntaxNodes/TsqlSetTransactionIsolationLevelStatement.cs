namespace QlParse;

// SET TRANSACTION ISOLATION LEVEL {READ UNCOMMITTED|READ COMMITTED|REPEATABLE READ|SNAPSHOT|SERIALIZABLE}.
public sealed class TsqlSetTransactionIsolationLevelStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public required SyntaxToken IsolationKeyword { get; init; }
    public required SyntaxToken LevelKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Level { get; init; }
}
