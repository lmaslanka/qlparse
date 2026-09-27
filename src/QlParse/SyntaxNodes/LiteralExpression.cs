namespace QlParse;

public sealed class LiteralExpression : Expression
{
    public required SyntaxToken Literal { get; init; }
}
