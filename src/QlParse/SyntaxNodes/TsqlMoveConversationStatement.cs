namespace QlParse;

// `MOVE CONVERSATION @handle TO @group`.
public sealed class TsqlMoveConversationStatement : Query
{
    public required SyntaxToken MoveKeyword { get; init; }
    public required SyntaxToken ConversationKeyword { get; init; }
    public required SyntaxToken Handle { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken Group { get; init; }
}
