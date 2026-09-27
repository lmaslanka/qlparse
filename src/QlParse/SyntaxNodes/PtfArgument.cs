namespace QlParse;

public sealed class PtfArgument
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? SemanticsKeyword { get; init; }
    public Query? Query { get; init; }
    public IReadOnlyList<Expression> Values { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionBy { get; init; }
    public IReadOnlyList<Expression> PartitionByList { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public SyntaxToken? PruneKeyword { get; init; }
    public Expression? Prune { get; init; }
    public SyntaxToken? CopartitionKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Names { get; init; } = [];
    public Expression? Scalar { get; init; }
}
