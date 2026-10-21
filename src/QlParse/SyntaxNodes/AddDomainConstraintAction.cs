namespace QlParse;

public sealed class AddDomainConstraintAction : AlterDomainAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required DomainConstraint Constraint { get; init; }

    // Postgres `NOT VALID`.
    public SyntaxToken? ValidNotKeyword { get; init; }
    public SyntaxToken? ValidKeyword { get; init; }
}
