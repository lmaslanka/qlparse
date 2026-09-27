namespace QlParse;

public sealed class NullIfExpression : Expression
{
    public required SyntaxToken NullIfKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
