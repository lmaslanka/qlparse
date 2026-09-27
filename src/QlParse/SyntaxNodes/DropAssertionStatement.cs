namespace QlParse;

public sealed class DropAssertionStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken AssertionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
