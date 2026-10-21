namespace QlParse;

// SQL Server-only parts of a data type: the schema collection of a typed `XML([CONTENT|DOCUMENT] collection)`.
// Allocated only for a typed XML type.
public sealed class TsqlDataTypeExtras
{
    public required IReadOnlyList<SyntaxToken> SchemaCollection { get; init; }
}
