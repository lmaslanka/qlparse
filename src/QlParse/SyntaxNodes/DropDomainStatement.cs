namespace QlParse;

public sealed class DropDomainStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
