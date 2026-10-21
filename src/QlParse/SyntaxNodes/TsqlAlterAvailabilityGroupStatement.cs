namespace QlParse;

// `ALTER AVAILABILITY GROUP name action`: Action holds the action's words (SET, ADD DATABASE,
// MODIFY REPLICA ON, JOIN AVAILABILITY GROUP ON, FAILOVER, GRANT CREATE ANY DATABASE, ...), Options the
// SET option list and Targets the databases, replicas, groups or listener the action names.
public sealed class TsqlAlterAvailabilityGroupStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken AvailabilityKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required IReadOnlyList<SyntaxToken> Action { get; init; }
    public TsqlOptionList? Options { get; init; }
    public IReadOnlyList<TsqlAvailabilityReplica> Targets { get; init; } = [];
}
