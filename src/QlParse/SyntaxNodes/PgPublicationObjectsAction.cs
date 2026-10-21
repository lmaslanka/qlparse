namespace QlParse;

public sealed class PgPublicationObjectsAction : PgAlterAction
{
    public required SyntaxToken Action { get; init; }
    public required IReadOnlyList<PgPublicationObject> Objects { get; init; }
}
