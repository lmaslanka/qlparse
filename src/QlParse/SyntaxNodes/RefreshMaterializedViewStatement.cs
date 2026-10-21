namespace QlParse;

public sealed class RefreshMaterializedViewStatement : Query
{
    public required SyntaxToken RefreshKeyword { get; init; }
    public required SyntaxToken MaterializedKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public SyntaxToken? ConcurrentlyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
}
