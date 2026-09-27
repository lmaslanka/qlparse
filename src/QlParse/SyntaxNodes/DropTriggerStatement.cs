namespace QlParse;

public sealed class DropTriggerStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}
