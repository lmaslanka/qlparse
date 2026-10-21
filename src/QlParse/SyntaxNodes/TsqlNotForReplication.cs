namespace QlParse;

// `NOT FOR REPLICATION` on an IDENTITY column, a FOREIGN KEY or a CHECK constraint.
public sealed class TsqlNotForReplication
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken NotKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken ReplicationKeyword { get; init; }
}
