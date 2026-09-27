namespace QlParse;

public sealed class DropPeriodAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
