namespace QlParse;

public sealed class MethodInvocationExpression : Expression
{
    public SyntaxToken? OpenParen { get; init; }
    public required Expression Target { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? Type { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken ArgumentsOpen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken ArgumentsClose { get; init; }
}
