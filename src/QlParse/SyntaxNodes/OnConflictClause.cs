namespace QlParse;

public sealed class OnConflictClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken ConflictKeyword { get; init; }
    public ConflictTarget? Target { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public SyntaxToken? NothingKeyword { get; init; }
    public SyntaxToken? UpdateKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SetClause> Assignments { get; init; } = [];
    public WhereClause? Where { get; init; }
}
