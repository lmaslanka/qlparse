namespace QlParse;

public sealed class AggregateCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public SyntaxToken? SetQuantifier { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public WithinGroupClause? WithinGroup { get; init; }
    public FilterClause? Filter { get; init; }
    public WindowSpecification? Over { get; init; }
}
