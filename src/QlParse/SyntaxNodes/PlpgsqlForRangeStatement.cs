namespace QlParse;

public sealed class PlpgsqlForRangeStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public SyntaxToken? ReverseKeyword { get; init; }
    public required Expression Lower { get; init; }
    public required SyntaxToken DotDot { get; init; }
    public required Expression Upper { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public Expression? Step { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
