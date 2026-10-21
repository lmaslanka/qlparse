namespace QlParse;

public sealed class CommitPreparedStatement : Query
{
    public required SyntaxToken CommitKeyword { get; init; }
    public required SyntaxToken PreparedKeyword { get; init; }
    public required SyntaxToken TransactionId { get; init; }
}
