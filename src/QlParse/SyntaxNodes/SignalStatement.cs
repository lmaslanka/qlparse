namespace QlParse;

public sealed class SignalStatement : Query
{
    public required SyntaxToken SignalKeyword { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
    public SyntaxToken? Condition { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SignalInformation> Items { get; init; } = [];
}
