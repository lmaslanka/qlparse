namespace QlParse;

public sealed class DropDomainConstraintAction : AlterDomainAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }

    // Null only for Postgres, where CASCADE / RESTRICT is optional.
    public SyntaxToken? Behavior { get; init; }
    public ExistenceCheck? IfExists { get; init; }
}
