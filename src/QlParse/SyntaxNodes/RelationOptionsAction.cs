namespace QlParse;

public sealed class RelationOptionsAction : AlterTableAction
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required StorageParameterList Parameters { get; init; }
}
