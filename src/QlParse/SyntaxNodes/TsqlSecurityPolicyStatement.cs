namespace QlParse;

// CREATE | ALTER SECURITY POLICY name predicate_action, ... [WITH (STATE = ..., SCHEMABINDING = ...)] [NOT FOR REPLICATION].
public sealed class TsqlSecurityPolicyStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken SecurityKeyword { get; init; }
    public required SyntaxToken PolicyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public IReadOnlyList<TsqlSecurityPredicate> Predicates { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlObjectOptionList? Options { get; init; }
    public IReadOnlyList<SyntaxToken> NotForReplication { get; init; } = [];
}
