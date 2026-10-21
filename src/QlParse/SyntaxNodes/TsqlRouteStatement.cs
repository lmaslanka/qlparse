namespace QlParse;

// `{CREATE | ALTER} ROUTE name [AUTHORIZATION owner] WITH option, ...`.
public sealed class TsqlRouteStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken RouteKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required TsqlOptionList Options { get; init; }
}
