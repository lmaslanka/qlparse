namespace QlParse;

public sealed class PlpgsqlAssignment : Query
{
    public required Expression Target { get; init; }
    public required SyntaxToken AssignmentToken { get; init; }
    public required Expression Value { get; init; }
}
