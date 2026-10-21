namespace QlParse;

// Postgres STORED / VIRTUAL after a generation expression; allocated only when present.
public sealed class PgGeneratedColumnExtras
{
    public SyntaxToken? StorageKeyword { get; init; }
}
