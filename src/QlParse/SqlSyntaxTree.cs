namespace QlParse;

public sealed class SqlSyntaxTree
{
    public required string Source { get; init; }
    public required Query Root { get; init; }
    public required IReadOnlyList<SyntaxToken> Tokens { get; init; }
    public required IReadOnlyList<SyntaxTrivia> Trivia { get; init; }
}
