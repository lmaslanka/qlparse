namespace QlParse;

// One `from_node TO to_node` pair of an edge constraint.
public sealed class TsqlEdgeConnection
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> FromNode { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ToNode { get; init; }
}
