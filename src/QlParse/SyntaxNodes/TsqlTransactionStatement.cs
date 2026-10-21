namespace QlParse;

// BEGIN [DISTRIBUTED] TRAN[SACTION] [name] [WITH MARK ['description']], SAVE TRAN[SACTION] name,
// COMMIT [TRAN[SACTION] [name] | WORK] [WITH (DELAYED_DURABILITY = {ON|OFF})] and ROLLBACK [TRAN[SACTION] [name] | WORK].
// TransactionKeyword is TRAN, TRANSACTION or WORK.
public sealed class TsqlTransactionStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public SyntaxToken? DistributedKeyword { get; init; }
    public SyntaxToken? TransactionKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? MarkKeyword { get; init; }
    public SyntaxToken? MarkDescription { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public TsqlOption? Durability { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
