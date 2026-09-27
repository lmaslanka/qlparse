namespace QlParse;

public sealed class JsonAccessorExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required Expression Index { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}
