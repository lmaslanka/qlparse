namespace QlParse;

// `BEGIN DIALOG [CONVERSATION] @handle FROM SERVICE initiator TO SERVICE 'target' [, 'instance']
// [ON CONTRACT contract] [WITH option, ...]`.
public sealed class TsqlBeginDialogStatement : Query
{
    public required SyntaxToken BeginKeyword { get; init; }
    public required SyntaxToken DialogKeyword { get; init; }
    public SyntaxToken? ConversationKeyword { get; init; }
    public required SyntaxToken Handle { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken FromServiceKeyword { get; init; }
    public required SyntaxToken InitiatorService { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken ToServiceKeyword { get; init; }
    public required SyntaxToken TargetService { get; init; }
    public SyntaxToken? BrokerInstance { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? ContractKeyword { get; init; }
    public SyntaxToken? Contract { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
