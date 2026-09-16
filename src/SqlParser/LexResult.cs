namespace SqlParser;

internal sealed class LexResult
{
    public required IReadOnlyList<SyntaxToken> Tokens { get; init; }
    public required IReadOnlyList<SyntaxTrivia> Trivia { get; init; }
    public SqlParseException? Error { get; init; }
}
