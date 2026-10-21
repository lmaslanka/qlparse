namespace QlParse;

public sealed class PlpgsqlReturnStatement : Query
{
    public required SyntaxToken ReturnKeyword { get; init; }
    public SyntaxToken? Modifier { get; init; }
    public Expression? Value { get; init; }
    public Query? Query { get; init; }
}
