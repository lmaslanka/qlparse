namespace QlParse;

public sealed class JoinClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NaturalKeyword { get; init; }
    public SyntaxToken? JoinType { get; init; }
    public SyntaxToken? OuterKeyword { get; init; }
    public required SyntaxToken JoinKeyword { get; init; }
    public required TableSource Table { get; init; }
    public JoinConstraint? Constraint { get; init; }
}
