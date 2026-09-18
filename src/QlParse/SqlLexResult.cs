namespace QlParse;

public sealed class SqlLexResult
{
    public required string Source { get; init; }
    public required IReadOnlyList<SyntaxToken> Tokens { get; init; }
    public required IReadOnlyList<SyntaxTrivia> Trivia { get; init; }
    public SqlParseException? Error { get; init; }
}
