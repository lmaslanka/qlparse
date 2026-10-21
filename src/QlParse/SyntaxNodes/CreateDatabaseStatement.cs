namespace QlParse;

public sealed class CreateDatabaseStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
}
