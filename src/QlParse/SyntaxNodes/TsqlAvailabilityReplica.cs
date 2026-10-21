namespace QlParse;

// `'server' [WITH (option, ...)]` in an availability group's replica, group or listener list; also a
// database name in ALTER AVAILABILITY GROUP ... ADD DATABASE, and `'listener' (option, ...)`.
public sealed class TsqlAvailabilityReplica
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
