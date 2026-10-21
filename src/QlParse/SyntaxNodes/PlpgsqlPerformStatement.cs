namespace QlParse;

public sealed class PlpgsqlPerformStatement : Query
{
    public required SyntaxToken PerformKeyword { get; init; }
    public required Query Query { get; init; }
}
