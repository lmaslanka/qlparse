namespace QlParse;

public sealed class ReplicaIdentityAction : AlterTableAction
{
    public required SyntaxToken ReplicaKeyword { get; init; }
    public required SyntaxToken IdentityKeyword { get; init; }
    public required SyntaxToken Mode { get; init; }
    public SyntaxToken? IndexKeyword { get; init; }
    public SyntaxToken? Index { get; init; }
}
