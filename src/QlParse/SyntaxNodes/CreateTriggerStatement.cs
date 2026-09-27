namespace QlParse;

public sealed class CreateTriggerStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ActionTime { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? ColumnsOfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? ReferencingKeyword { get; init; }
    public IReadOnlyList<TransitionClause> Transitions { get; init; } = [];
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? EachKeyword { get; init; }
    public SyntaxToken? Granularity { get; init; }
    public SyntaxToken? WhenKeyword { get; init; }
    public SyntaxToken? WhenOpen { get; init; }
    public Expression? When { get; init; }
    public SyntaxToken? WhenClose { get; init; }
    public required Query Body { get; init; }
}
