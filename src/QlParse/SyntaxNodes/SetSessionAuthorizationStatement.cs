namespace QlParse;

public sealed class SetSessionAuthorizationStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SessionKeyword { get; init; }
    public required SyntaxToken AuthorizationKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
