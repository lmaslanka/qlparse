namespace QlParse;

// Postgres `SET TRANSACTION SNAPSHOT 'id'` (Modes is then empty); allocated only for that form.
public sealed class PgSetTransactionExtras
{
    public SyntaxToken? SnapshotKeyword { get; init; }
    public SyntaxToken? SnapshotId { get; init; }
}
