namespace QlParse;

// `{message_type | [DEFAULT]} SENT BY {INITIATOR | TARGET | ANY}` in CREATE CONTRACT.
public sealed class TsqlContractMessage
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken MessageType { get; init; }
    public required SyntaxToken SentKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required SyntaxToken Sender { get; init; }
}
