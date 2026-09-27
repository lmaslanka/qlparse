namespace QlParse;

public sealed class WindowSpecification
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OverKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionByKeyword { get; init; }
    public IReadOnlyList<Expression> PartitionBy { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public WindowFrame? Frame { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
