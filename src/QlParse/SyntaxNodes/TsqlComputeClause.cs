namespace QlParse;

// Legacy `COMPUTE aggregate, ... [BY expression, ...]` (parse-only).
public sealed class TsqlComputeClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ComputeKeyword { get; init; }
    public required IReadOnlyList<Expression> Functions { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public IReadOnlyList<Expression> By { get; init; } = [];
}
