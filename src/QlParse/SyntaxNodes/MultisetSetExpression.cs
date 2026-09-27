namespace QlParse;

public sealed class MultisetSetExpression : Expression
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
