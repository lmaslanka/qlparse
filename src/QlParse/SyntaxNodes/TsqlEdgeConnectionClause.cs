namespace QlParse;

// Edge constraint body: `CONNECTION (node TO node, ...)`; its ON DELETE action is in the constraint's Actions.
public sealed class TsqlEdgeConnectionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ConnectionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlEdgeConnection> Connections { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
