namespace QlParse;

public sealed class FetchStatement : Query
{
    public required SyntaxToken FetchKeyword { get; init; }
    public SyntaxToken? Orientation { get; init; }
    public Expression? Offset { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Targets { get; init; }
}
