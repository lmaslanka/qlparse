namespace QlParse;

// `END CONVERSATION @handle [WITH ERROR = code DESCRIPTION = 'text' | WITH CLEANUP]`.
public sealed class TsqlEndConversationStatement : Query
{
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken ConversationKeyword { get; init; }
    public required SyntaxToken Handle { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? ErrorKeyword { get; init; }
    public SyntaxToken? ErrorEquals { get; init; }
    public SyntaxToken? ErrorCode { get; init; }
    public SyntaxToken? DescriptionKeyword { get; init; }
    public SyntaxToken? DescriptionEquals { get; init; }
    public SyntaxToken? Description { get; init; }
    public SyntaxToken? CleanupKeyword { get; init; }
}
