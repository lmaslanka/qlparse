namespace QlParse;

public sealed class AlterTablespaceStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken TablespaceKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required AlterTableAction Action { get; init; }
}
