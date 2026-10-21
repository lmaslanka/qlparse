namespace QlParse;

// A graph path aggregate: `function(...) WITHIN GROUP (GRAPH PATH)` (STRING_AGG, COUNT, LAST_VALUE, ...).
public sealed class TsqlGraphPathAggregateExpression : Expression
{
    public required Expression Call { get; init; }
    public required SyntaxToken WithinKeyword { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken GraphKeyword { get; init; }
    public required SyntaxToken PathKeyword { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
