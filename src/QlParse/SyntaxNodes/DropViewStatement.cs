namespace QlParse;

public sealed class DropViewStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Behavior { get; init; }
    public SyntaxToken? MaterializedKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames { get; init; } = [];
}
