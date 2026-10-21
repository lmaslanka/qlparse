namespace QlParse;

public sealed class SetRelationSchemaAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required SyntaxToken Schema { get; init; }
}
