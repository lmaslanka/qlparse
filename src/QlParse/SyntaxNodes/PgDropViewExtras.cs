namespace QlParse;

// Postgres-only parts of DROP [MATERIALIZED] VIEW (MATERIALIZED, IF EXISTS, further names);
// allocated only when at least one is present.
public sealed class PgDropViewExtras
{
    public SyntaxToken? MaterializedKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames { get; init; } = [];
}
