namespace QlParse;

public sealed class DropCollationStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
