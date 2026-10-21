namespace QlParse;

public sealed class PgSetSchemaAction : PgAlterAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required SyntaxToken Schema { get; init; }
}
