namespace QlParse;

public sealed class InExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
