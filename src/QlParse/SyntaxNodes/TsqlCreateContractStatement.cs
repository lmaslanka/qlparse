namespace QlParse;

// `CREATE CONTRACT name [AUTHORIZATION owner] (message SENT BY sender, ...)`.
public sealed class TsqlCreateContractStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ContractKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlContractMessage> Messages { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
