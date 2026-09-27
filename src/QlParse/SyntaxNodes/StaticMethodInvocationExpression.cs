namespace QlParse;

public sealed class StaticMethodInvocationExpression : Expression
{
    public required Expression Type { get; init; }
    public required SyntaxToken DoubleColon { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
