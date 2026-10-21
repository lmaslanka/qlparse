namespace QlParse;

// `GET CONVERSATION GROUP @group FROM queue`; also the body of WAITFOR (GET CONVERSATION GROUP ...).
public sealed class TsqlGetConversationGroupStatement : Query
{
    public required SyntaxToken GetKeyword { get; init; }
    public required SyntaxToken ConversationKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken Group { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Queue { get; init; }
}
