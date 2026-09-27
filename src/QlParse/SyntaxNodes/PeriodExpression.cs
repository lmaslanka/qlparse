namespace QlParse;

public sealed class PeriodExpression : Expression
{
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Start { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required Expression End { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
