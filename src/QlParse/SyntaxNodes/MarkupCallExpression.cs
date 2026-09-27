namespace QlParse;

public sealed class MarkupCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Clauses { get; init; } = [];
    public DataType? Returning { get; init; }
    public Query? Query { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public IReadOnlyList<MarkupColumn> Columns { get; init; } = [];
    public FilterClause? Filter { get; init; }
    public WindowSpecification? Over { get; init; }
}
