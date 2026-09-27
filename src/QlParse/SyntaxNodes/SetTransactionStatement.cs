namespace QlParse;

public sealed class SetTransactionStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? LocalKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public required IReadOnlyList<TransactionMode> Modes { get; init; }
}
