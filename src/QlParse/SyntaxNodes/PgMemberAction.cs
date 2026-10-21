namespace QlParse;

public sealed class PgMemberAction : PgAlterAction
{
    public required SyntaxToken Action { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public required PgObjectReference Member { get; init; }
}
