namespace QlParse;

public sealed class DropColumnAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
