namespace QlParse;

public sealed class RowConstructorExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
