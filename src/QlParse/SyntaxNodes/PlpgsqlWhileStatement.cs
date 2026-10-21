namespace QlParse;

public sealed class PlpgsqlWhileStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required SyntaxToken WhileKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
