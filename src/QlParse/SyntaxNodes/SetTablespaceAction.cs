namespace QlParse;

public sealed class SetTablespaceAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken TablespaceKeyword { get; init; }
    public required SyntaxToken Tablespace { get; init; }
    public SyntaxToken? NowaitKeyword { get; init; }
}
