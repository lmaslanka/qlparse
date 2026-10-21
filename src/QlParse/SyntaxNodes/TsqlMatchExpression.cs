namespace QlParse;

// Graph `MATCH (pattern [AND pattern ...])` search condition.
public sealed class TsqlMatchExpression : Expression
{
    public required SyntaxToken MatchKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlGraphPattern> Patterns { get; init; }
    public IReadOnlyList<SyntaxToken> AndKeywords { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
