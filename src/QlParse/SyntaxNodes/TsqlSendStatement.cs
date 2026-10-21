namespace QlParse;

// `SEND ON CONVERSATION [(]@handle, ...[)] [MESSAGE TYPE type] [(body)]`.
public sealed class TsqlSendStatement : Query
{
    public required SyntaxToken SendKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken ConversationKeyword { get; init; }
    public SyntaxToken? HandlesOpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Handles { get; init; }
    public SyntaxToken? HandlesCloseParen { get; init; }
    public SyntaxToken? MessageKeyword { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public SyntaxToken? MessageType { get; init; }
    public SyntaxToken? BodyOpenParen { get; init; }
    public Expression? Body { get; init; }
    public SyntaxToken? BodyCloseParen { get; init; }
}
