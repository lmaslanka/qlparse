namespace QlParse;

public sealed class LikeClause : TableElement
{
    public required SyntaxToken LikeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public IReadOnlyList<LikeOption> Options { get; init; } = [];
}
