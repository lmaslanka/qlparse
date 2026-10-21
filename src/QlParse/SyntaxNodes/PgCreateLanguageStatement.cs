namespace QlParse;

public sealed class PgCreateLanguageStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public SyntaxToken? TrustedKeyword { get; init; }
    public SyntaxToken? ProceduralKeyword { get; init; }
    public required SyntaxToken LanguageKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<PgHandlerClause> Handlers { get; init; } = [];
}
