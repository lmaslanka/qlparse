namespace QlParse;

public sealed class DropViewStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
