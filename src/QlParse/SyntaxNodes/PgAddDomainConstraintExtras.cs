namespace QlParse;

// Postgres `NOT VALID` after an added domain constraint;
// allocated only when at least one is present.
public sealed class PgAddDomainConstraintExtras
{
    public SyntaxToken? ValidNotKeyword { get; init; }
    public SyntaxToken? ValidKeyword { get; init; }
}
