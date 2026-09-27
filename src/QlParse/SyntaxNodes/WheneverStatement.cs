namespace QlParse;

public sealed class WheneverStatement : Query
{
    public required SyntaxToken WheneverKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Condition { get; init; }
    public required SyntaxToken Action { get; init; }
    public SyntaxToken? GoKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? Target { get; init; }
}
