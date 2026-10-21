namespace QlParse;

// {ENABLE | DISABLE} TRIGGER {name, ... | ALL} ON {object | DATABASE | ALL SERVER}.
public sealed class TsqlToggleTriggerStatement : Query
{
    public required SyntaxToken ToggleKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public SyntaxToken? AllTriggersKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Triggers { get; init; } = [];
    public required SyntaxToken OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Target { get; init; } = [];
    public SyntaxToken? DatabaseKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? ServerKeyword { get; init; }
}
