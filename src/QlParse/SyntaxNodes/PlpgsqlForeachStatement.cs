namespace QlParse;

public sealed class PlpgsqlForeachStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required SyntaxToken ForeachKeyword { get; init; }
    public IReadOnlyList<Expression> Targets { get; init; } = [];
    public SyntaxToken? SliceKeyword { get; init; }
    public SyntaxToken? Slice { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken ArrayKeyword { get; init; }
    public required Expression Array { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
