namespace QlParse;

public sealed class DropTypeStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
