namespace QlParse;

public sealed class StarExpression : Expression
{
    public required SyntaxToken Star { get; init; }
}
