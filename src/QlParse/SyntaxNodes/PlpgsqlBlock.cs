namespace QlParse;

public sealed class PlpgsqlBlock : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public SyntaxToken? DeclareKeyword { get; init; }
    public IReadOnlyList<Query> Declarations { get; init; } = [];
    public required SyntaxToken BeginKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public SyntaxToken? ExceptionKeyword { get; init; }
    public IReadOnlyList<PlpgsqlExceptionHandler> Handlers { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
