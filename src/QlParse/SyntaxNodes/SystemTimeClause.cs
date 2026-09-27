namespace QlParse;

public sealed class SystemTimeClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken SystemTimeKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public Expression? Point { get; init; }
    public SyntaxToken? BetweenKeyword { get; init; }
    public SyntaxToken? Qualifier { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public Expression? Start { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public Expression? End { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
}
