namespace QlParse;

// Postgres-only parts of a column definition (`STORAGE mode`, `COMPRESSION method`);
// allocated only when at least one is present.
public sealed class PgColumnExtras
{
    public SyntaxToken? StorageKeyword { get; init; }
    public SyntaxToken? StorageMode { get; init; }
    public SyntaxToken? CompressionKeyword { get; init; }
    public SyntaxToken? CompressionMethod { get; init; }
}
