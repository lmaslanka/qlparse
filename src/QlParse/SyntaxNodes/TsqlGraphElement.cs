namespace QlParse;

// One element of a graph MATCH pattern: a node, an edge step, or a quantified group.
public abstract class TsqlGraphElement
{
    public required SourceSpan Span { get; init; }
}
