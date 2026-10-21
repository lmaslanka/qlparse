namespace QlParse;

// REVERT [WITH COOKIE = @cookie].
public sealed class TsqlRevertStatement : Query
{
    public required SyntaxToken RevertKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? CookieKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Cookie { get; init; }
}
