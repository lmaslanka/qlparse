namespace QlParse;

// `CREATE AVAILABILITY GROUP name [WITH (option, ...)] [FOR [DATABASE db, ...]]
// {REPLICA ON | AVAILABILITY GROUP ON} replica, ... [LISTENER 'name' (option, ...)]`.
public sealed class TsqlCreateAvailabilityGroupStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AvailabilityKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? DatabaseKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Databases { get; init; } = [];
    public required IReadOnlyList<SyntaxToken> ReplicaKeywords { get; init; }
    public required IReadOnlyList<TsqlAvailabilityReplica> Replicas { get; init; }
    public SyntaxToken? ListenerKeyword { get; init; }
    public TsqlAvailabilityReplica? Listener { get; init; }
}
