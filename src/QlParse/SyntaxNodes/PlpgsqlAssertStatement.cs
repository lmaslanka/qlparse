namespace QlParse;

public sealed class PlpgsqlAssertStatement : Query
{
    public required SyntaxToken AssertKeyword { get; init; }
    public required Expression Condition { get; init; }
    public Expression? Message { get; init; }
}
