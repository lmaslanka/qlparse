namespace QlParse;

public sealed class RepeatStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken RepeatKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken UntilKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndRepeatKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
