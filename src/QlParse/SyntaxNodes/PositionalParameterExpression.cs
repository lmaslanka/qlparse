namespace QlParse;

public sealed class PositionalParameterExpression : Expression
{
    public required SyntaxToken Parameter { get; init; }
}
