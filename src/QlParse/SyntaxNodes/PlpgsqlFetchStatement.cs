namespace QlParse;

public sealed class PlpgsqlFetchStatement : Query
{
    public required SyntaxToken Keyword { get; init; }
    public IReadOnlyList<SyntaxToken> Direction { get; init; } = [];
    public Expression? Count { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public IReadOnlyList<Expression> Targets { get; init; } = [];
}
