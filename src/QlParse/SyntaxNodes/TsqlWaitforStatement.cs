namespace QlParse;

// WAITFOR {DELAY | TIME} value, or WAITFOR (statement) [, TIMEOUT value] around RECEIVE / GET CONVERSATION GROUP.
public sealed class TsqlWaitforStatement : Query
{
    public required SyntaxToken WaitforKeyword { get; init; }
    public SyntaxToken? DelayOrTimeKeyword { get; init; }
    public Expression? Value { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public Query? Statement { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? Comma { get; init; }
    public SyntaxToken? TimeoutKeyword { get; init; }
    public Expression? Timeout { get; init; }
}
