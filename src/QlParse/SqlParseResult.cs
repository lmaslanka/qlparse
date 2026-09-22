namespace QlParse;

public sealed class SqlParseResult
{
    public required string Source { get; init; }
    public Query? Root { get; init; }
    public required IReadOnlyList<SyntaxToken> Tokens { get; init; }
    public required IReadOnlyList<SyntaxTrivia> Trivia { get; init; }
    public SqlParseException? Error { get; init; }
}
