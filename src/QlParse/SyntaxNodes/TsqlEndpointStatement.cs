namespace QlParse;

// `{CREATE | ALTER} ENDPOINT name [AUTHORIZATION login] [STATE = state] [AS {TCP | HTTP} (option, ...)]
// [FOR {TSQL | SERVICE_BROKER | DATABASE_MIRRORING} (option, ...)]`.
public sealed class TsqlEndpointStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken EndpointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public TsqlOption? State { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Protocol { get; init; }
    public TsqlOptionList? ProtocolOptions { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? Payload { get; init; }
    public TsqlOptionList? PayloadOptions { get; init; }
}
