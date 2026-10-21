namespace QlParse;

// Postgres locking-clause parts: NO KEY / KEY strength, NOWAIT / SKIP LOCKED, and the next FOR clause.
public sealed class PgLockExtras
{
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
    public SyntaxToken? SkipKeyword { get; init; }
    public SyntaxToken? LockedKeyword { get; init; }
    public LockClause? NextLock { get; init; }
}
