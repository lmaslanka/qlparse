namespace QlParse;

public sealed class ParenExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
