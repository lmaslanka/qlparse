namespace QlParse;

public sealed class PlpgsqlLoopBody
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken LoopKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndLoopKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
