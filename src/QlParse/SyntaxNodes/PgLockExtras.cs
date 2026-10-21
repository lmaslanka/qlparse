namespace QlParse;

// Postgres locking-clause parts: NO KEY / KEY strength, NOWAIT / SKIP LOCKED, the next FOR clause,
// and the full `OF` names when at least one is qualified (LockClause.Columns then holds each last part).
public sealed class PgLockExtras
{
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
    public SyntaxToken? SkipKeyword { get; init; }
    public SyntaxToken? LockedKeyword { get; init; }
    public LockClause? NextLock { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> OfNames { get; init; } = [];
}
