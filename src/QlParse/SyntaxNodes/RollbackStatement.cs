namespace QlParse;

public sealed class RollbackStatement : Query
{
    public required SyntaxToken RollbackKeyword { get; init; }
    public SyntaxToken? WorkKeyword { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ChainKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? SavepointKeyword { get; init; }
    public SyntaxToken? Savepoint { get; init; }
}
