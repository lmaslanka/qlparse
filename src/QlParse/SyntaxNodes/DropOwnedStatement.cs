namespace QlParse;

public sealed class DropOwnedStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken OwnedKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Roles { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
