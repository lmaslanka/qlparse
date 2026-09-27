namespace QlParse;

public sealed class DropConstraintAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
