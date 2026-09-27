namespace QlParse;

public sealed class CompoundStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken BeginKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? AtomicKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}
