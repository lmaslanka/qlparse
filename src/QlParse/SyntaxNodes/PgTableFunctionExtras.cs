namespace QlParse;

// Postgres WITH ORDINALITY and `AS alias (column type, ...)` column definitions on a FROM function.
public sealed class PgTableFunctionExtras
{
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OrdinalityKeyword { get; init; }
    public IReadOnlyList<FieldDefinition> ColumnDefinitions { get; init; } = [];
}
