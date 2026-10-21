namespace QlParse;

// CREATE EVENT NOTIFICATION name ON {SERVER | DATABASE | QUEUE queue} [WITH FAN_IN] FOR event[, ...]
// TO SERVICE 'service', {'broker_instance' | 'current database'}.
public sealed class TsqlCreateEventNotificationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken EventKeyword { get; init; }
    public required SyntaxToken NotificationKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Scope { get; init; }
    public IReadOnlyList<SyntaxToken> Queue { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? FanInKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Events { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken ServiceKeyword { get; init; }
    public required SyntaxToken Service { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required SyntaxToken BrokerInstance { get; init; }
}
