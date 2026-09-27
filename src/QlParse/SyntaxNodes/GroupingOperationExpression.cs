namespace QlParse;

public sealed class GroupingOperationExpression : Expression
{
    public required SyntaxToken Keyword { get; init; }
    public SyntaxToken? SetsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
