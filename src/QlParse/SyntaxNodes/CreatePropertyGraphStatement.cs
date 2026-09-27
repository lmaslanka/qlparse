namespace QlParse;

public sealed class CreatePropertyGraphStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken GraphKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken VertexKeyword { get; init; }
    public required IReadOnlyList<GraphTableElement> Vertices { get; init; }
    public SyntaxToken? EdgeKeyword { get; init; }
    public IReadOnlyList<GraphTableElement> Edges { get; init; } = [];
}
