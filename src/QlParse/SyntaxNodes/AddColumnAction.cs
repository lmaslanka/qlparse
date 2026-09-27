namespace QlParse;

public sealed class AddColumnAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required ColumnDefinition Column { get; init; }
}
