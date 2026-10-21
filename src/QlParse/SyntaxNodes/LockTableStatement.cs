namespace QlParse;

public sealed class LockTableStatement : Query
{
    public required SyntaxToken LockKeyword { get; init; }
    public SyntaxToken? TableKeyword { get; init; }
    public required IReadOnlyList<LockTarget> Tables { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> LockMode { get; init; } = [];
    public SyntaxToken? ModeKeyword { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
}
