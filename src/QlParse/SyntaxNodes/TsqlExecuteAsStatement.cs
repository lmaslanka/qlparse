namespace QlParse;

// EXEC[UTE] AS {CALLER | SELF | OWNER | USER = 'name' | LOGIN = 'name'} [WITH {NO REVERT | COOKIE INTO @cookie}].
// WithKeywords is `NO REVERT` or `COOKIE INTO`, and Cookie the variable after INTO.
public sealed class TsqlExecuteAsStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken ContextKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Principal { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> WithKeywords { get; init; } = [];
    public SyntaxToken? Cookie { get; init; }
}
