namespace QlParse;

public sealed class PgRenameAction : PgAlterAction
{
    public required SyntaxToken RenameKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public SyntaxToken? OldName { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken NewName { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
