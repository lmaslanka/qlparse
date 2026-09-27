namespace QlParse;

public sealed class DomainConstraint
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public required SyntaxToken CheckKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Check { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}
