namespace QlParse;

public sealed class PgCreateAccessMethodStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AccessKeyword { get; init; }
    public required SyntaxToken MethodKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken HandlerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Handler { get; init; }
}
