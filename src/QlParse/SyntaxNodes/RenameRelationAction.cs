namespace QlParse;

public sealed class RenameRelationAction : AlterTableAction
{
    public required SyntaxToken RenameKeyword { get; init; }
    public SyntaxToken? TargetKeyword { get; init; }
    public SyntaxToken? Target { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken NewName { get; init; }
}
