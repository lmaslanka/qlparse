namespace QlParse;

public sealed class RowLevelSecurityAction : AlterTableAction
{
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken RowKeyword { get; init; }
    public required SyntaxToken LevelKeyword { get; init; }
    public required SyntaxToken SecurityKeyword { get; init; }
}
