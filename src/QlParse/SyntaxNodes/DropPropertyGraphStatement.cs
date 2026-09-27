namespace QlParse;

public sealed class DropPropertyGraphStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken GraphKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
