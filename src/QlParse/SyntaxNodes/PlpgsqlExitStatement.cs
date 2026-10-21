namespace QlParse;

public sealed class PlpgsqlExitStatement : Query
{
    public required SyntaxToken Keyword { get; init; }
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? WhenKeyword { get; init; }
    public Expression? Condition { get; init; }
}
