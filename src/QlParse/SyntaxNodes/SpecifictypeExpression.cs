namespace QlParse;

public sealed class SpecifictypeExpression : Expression
{
    public required SyntaxToken SpecifictypeKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
