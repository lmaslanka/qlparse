namespace QlParse;

public sealed class DirectSqlScript : Query
{
    public required IReadOnlyList<Query> Statements { get; init; }
    public required IReadOnlyList<SyntaxToken> Semicolons { get; init; }
}
