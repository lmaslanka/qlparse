namespace QlParse;

public sealed class AlterViewRelationAction : AlterViewAction
{
    public required AlterTableAction Action { get; init; }
}
