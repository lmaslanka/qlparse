namespace QlParse;

// One MATCH pattern: a node/edge chain, or `SHORTEST_PATH(...)` around a chain with a quantified group.
public sealed class TsqlGraphPattern
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? ShortestPathKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<TsqlGraphElement> Elements { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
