namespace QlParse;

// `KILL {session_id | 'uow'} [WITH STATUSONLY]`, `KILL QUERY NOTIFICATION SUBSCRIPTION {ALL | id}` and
// `KILL STATS JOB id`. Qualifier holds the words between KILL and the target.
public sealed class TsqlKillStatement : Query
{
    public required SyntaxToken KillKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Qualifier { get; init; } = [];
    public required SyntaxToken Target { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? StatusOnlyKeyword { get; init; }
}
