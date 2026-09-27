namespace QlParse;

public sealed class CoalesceExpression : Expression
{
    public required SyntaxToken CoalesceKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
