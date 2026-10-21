namespace QlParse;

public sealed class ConflictTarget
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<ConflictTargetElement> Elements { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public WhereClause? Where { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
}
