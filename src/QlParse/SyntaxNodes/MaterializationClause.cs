namespace QlParse;

public sealed class MaterializationClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken MaterializedKeyword { get; init; }
}
