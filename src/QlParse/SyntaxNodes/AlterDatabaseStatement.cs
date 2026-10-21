namespace QlParse;

public sealed class AlterDatabaseStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> ActionKeywords { get; init; } = [];
    public SyntaxToken? Target { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public Query? Setting { get; init; }
}
