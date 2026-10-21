namespace QlParse;

// The Postgres operator class of an index column, with its optional parameter list.
// Allocated only when an operator class is present.
public sealed class PgIndexColumnExtras
{
    public IReadOnlyList<SyntaxToken>? OperatorClass { get; init; }
    public StorageParameterList? OperatorClassParameters { get; init; }
}
