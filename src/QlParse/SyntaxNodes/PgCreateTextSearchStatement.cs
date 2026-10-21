namespace QlParse;

public sealed class PgCreateTextSearchStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TextKeyword { get; init; }
    public required SyntaxToken SearchKeyword { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required PgDefinitionList Definition { get; init; }
}
