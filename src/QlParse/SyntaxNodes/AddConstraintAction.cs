namespace QlParse;

public sealed class AddConstraintAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required TableConstraint Constraint { get; init; }
}
