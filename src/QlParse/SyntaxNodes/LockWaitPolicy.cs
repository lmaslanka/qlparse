namespace QlParse;

// NOWAIT, or SKIP LOCKED.
public sealed class LockWaitPolicy
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
    public SyntaxToken? SkipKeyword { get; init; }
    public SyntaxToken? LockedKeyword { get; init; }
}
