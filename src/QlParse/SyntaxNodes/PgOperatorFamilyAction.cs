namespace QlParse;

public sealed class PgOperatorFamilyAction : PgAlterAction
{
    public required SyntaxToken Action { get; init; }
    public required IReadOnlyList<PgOperatorClassItem> Items { get; init; }
}
