namespace QlParse;

public sealed class WhileStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken WhileKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndWhileKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
