namespace QlParse;

public sealed class PlpgsqlForQueryStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public IReadOnlyList<Expression> Targets { get; init; } = [];
    public required SyntaxToken InKeyword { get; init; }
    public required Query Query { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
