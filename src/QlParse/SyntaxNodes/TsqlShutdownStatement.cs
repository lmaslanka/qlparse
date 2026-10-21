namespace QlParse;

// `SHUTDOWN [WITH NOWAIT]`.
public sealed class TsqlShutdownStatement : Query
{
    public required SyntaxToken ShutdownKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
}
