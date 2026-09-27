namespace QlParse;

public sealed class FunctionCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? FromPosition { get; init; }
    public SyntaxToken? NullTreatment { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public FilterClause? Filter { get; init; }
    public WindowSpecification? Over { get; init; }
}
