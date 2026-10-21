namespace QlParse;

public sealed class NotifyStatement : Query
{
    public required SyntaxToken NotifyKeyword { get; init; }
    public required SyntaxToken Channel { get; init; }
    public SyntaxToken? Comma { get; init; }
    public SyntaxToken? Payload { get; init; }
}
