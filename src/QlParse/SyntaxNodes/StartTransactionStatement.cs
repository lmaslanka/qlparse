namespace QlParse;

public sealed class StartTransactionStatement : Query
{
    public required SyntaxToken StartKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public IReadOnlyList<TransactionMode> Modes { get; init; } = [];
}
