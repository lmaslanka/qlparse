namespace QlParse;

// `BEGIN CONVERSATION TIMER (@handle) TIMEOUT = seconds`.
public sealed class TsqlBeginConversationTimerStatement : Query
{
    public required SyntaxToken BeginKeyword { get; init; }
    public required SyntaxToken ConversationKeyword { get; init; }
    public required SyntaxToken TimerKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Handle { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken TimeoutKeyword { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required SyntaxToken Timeout { get; init; }
}
