namespace QlParse;

public sealed class TreatExpression : Expression
{
    public required SyntaxToken TreatKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
