namespace QlParse;

public sealed class PrepareTransactionStatement : Query
{
    public required SyntaxToken PrepareKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public required SyntaxToken TransactionId { get; init; }
}
