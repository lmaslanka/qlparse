namespace QlParse;

public sealed class CommitStatement : Query
{
    public required SyntaxToken CommitKeyword { get; init; }
    public SyntaxToken? WorkKeyword { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ChainKeyword { get; init; }
}
