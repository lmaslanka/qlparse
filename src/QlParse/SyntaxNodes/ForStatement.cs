namespace QlParse;

public sealed class ForStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public SyntaxToken? CursorName { get; init; }
    public SyntaxToken? CursorKeyword { get; init; }
    public SyntaxToken? CursorForKeyword { get; init; }
    public required Query CursorQuery { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndForKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
