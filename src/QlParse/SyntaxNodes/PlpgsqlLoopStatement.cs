namespace QlParse;

public sealed class PlpgsqlLoopStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
