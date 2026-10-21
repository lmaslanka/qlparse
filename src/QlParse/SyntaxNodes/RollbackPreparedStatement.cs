namespace QlParse;

public sealed class RollbackPreparedStatement : Query
{
    public required SyntaxToken RollbackKeyword { get; init; }
    public required SyntaxToken PreparedKeyword { get; init; }
    public required SyntaxToken TransactionId { get; init; }
}
