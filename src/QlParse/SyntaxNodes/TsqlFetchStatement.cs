namespace QlParse;

// FETCH [[NEXT|PRIOR|FIRST|LAST|ABSOLUTE n|RELATIVE n] FROM] [GLOBAL] cursor [INTO @a, ...].
public sealed class TsqlFetchStatement : Query
{
    public required SyntaxToken FetchKeyword { get; init; }
    public SyntaxToken? Orientation { get; init; }
    public Expression? Offset { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? GlobalKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Variables { get; init; } = [];
}
