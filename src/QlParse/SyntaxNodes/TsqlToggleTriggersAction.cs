namespace QlParse;

// `{ENABLE|DISABLE} TRIGGER {ALL|name, ...}`.
public sealed class TsqlToggleTriggersAction : AlterTableAction
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Names { get; init; } = [];
}
