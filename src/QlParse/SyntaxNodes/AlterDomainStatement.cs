namespace QlParse;

public sealed class AlterDomainStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterDomainAction Action { get; init; }
}
