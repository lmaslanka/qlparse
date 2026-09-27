namespace QlParse;

public sealed class AddDomainConstraintAction : AlterDomainAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required DomainConstraint Constraint { get; init; }
}
