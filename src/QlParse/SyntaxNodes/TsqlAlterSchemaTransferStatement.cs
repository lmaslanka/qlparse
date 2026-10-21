namespace QlParse;

// ALTER SCHEMA target TRANSFER [class ::] securable.
public sealed class TsqlAlterSchemaTransferStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken TransferKeyword { get; init; }
    public required TsqlSecurable Securable { get; init; }
}
