namespace QlParse;

public sealed class BeginTransactionStatement : Query
{
    public required SyntaxToken BeginKeyword { get; init; }
    public SyntaxToken? TransactionKeyword { get; init; }
    public IReadOnlyList<TransactionMode> Modes { get; init; } = [];
}
