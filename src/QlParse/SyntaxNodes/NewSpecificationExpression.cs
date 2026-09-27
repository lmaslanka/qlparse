namespace QlParse;

public sealed class NewSpecificationExpression : Expression
{
    public required SyntaxToken NewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TypeName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
