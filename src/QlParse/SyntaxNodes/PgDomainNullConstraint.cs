namespace QlParse;

public sealed class PgDomainNullConstraint
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken NullKeyword { get; init; }
}
