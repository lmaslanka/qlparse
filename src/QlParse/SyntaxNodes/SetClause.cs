namespace QlParse;

public sealed class SetClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<SetTarget> Targets { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public Expression? Value { get; init; }
}
