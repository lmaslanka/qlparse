namespace QlParse;

public sealed class PlpgsqlNullStatement : Query
{
    public required SyntaxToken NullKeyword { get; init; }
}
