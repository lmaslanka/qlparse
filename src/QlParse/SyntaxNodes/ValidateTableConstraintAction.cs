namespace QlParse;

public sealed class ValidateTableConstraintAction : AlterTableAction
{
    public required SyntaxToken ValidateKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
