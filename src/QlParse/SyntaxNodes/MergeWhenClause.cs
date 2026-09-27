namespace QlParse;

public sealed class MergeWhenClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken MatchedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? ByKind { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public Expression? Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public required MergeAction Action { get; init; }
}
