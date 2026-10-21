namespace QlParse;

public sealed class DiscardStatement : Query
{
    public required SyntaxToken DiscardKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}
