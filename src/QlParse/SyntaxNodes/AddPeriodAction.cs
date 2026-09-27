namespace QlParse;

public sealed class AddPeriodAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required PeriodDefinition Period { get; init; }
}
