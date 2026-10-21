namespace QlParse;

// Postgres-only parts of a table constraint (EXCLUDE, index parameters, USING INDEX, NO INHERIT, NOT VALID);
// allocated only when at least one is present.
public sealed class PgTableConstraintExtras
{
    public ExclusionClause? Exclusion { get; init; }
    public IndexParameters? IndexParameters { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? IndexKeyword { get; init; }
    public SyntaxToken? IndexName { get; init; }
    public SyntaxToken? InheritNoKeyword { get; init; }
    public SyntaxToken? InheritKeyword { get; init; }
    public SyntaxToken? ValidNotKeyword { get; init; }
    public SyntaxToken? ValidKeyword { get; init; }
}
