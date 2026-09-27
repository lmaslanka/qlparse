namespace QlParse;

public sealed class DerefExpression : Expression
{
    public required SyntaxToken DerefKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
