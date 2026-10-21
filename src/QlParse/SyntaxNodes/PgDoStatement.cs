namespace QlParse;

public sealed class PgDoStatement : Query
{
    public required SyntaxToken DoKeyword { get; init; }
    public SyntaxToken? LanguageKeyword { get; init; }
    public SyntaxToken? Language { get; init; }
    public required SyntaxToken Code { get; init; }
    public Query? Body { get; init; }
    public IReadOnlyList<SyntaxToken> BodyTokens { get; init; } = [];
    public IReadOnlyList<SyntaxTrivia> BodyTrivia { get; init; } = [];
}
