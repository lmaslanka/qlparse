namespace SqlParser;

public sealed class SqlTrace
{
    public required string Lexer { get; init; }
    public string? Parser { get; init; }
    public string? Format { get; init; }
    public SqlParseException? Error { get; init; }
    public required SqlStats Stats { get; init; }
}
