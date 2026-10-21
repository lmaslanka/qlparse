namespace QlParse;

public sealed class ToggleTriggerAction : AlterTableAction
{
    public required SyntaxToken VerbKeyword { get; init; }
    public SyntaxToken? ModeKeyword { get; init; }
    public required SyntaxToken ObjectKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}
