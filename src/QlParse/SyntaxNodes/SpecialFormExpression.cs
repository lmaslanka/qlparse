namespace QlParse;

public sealed class SpecialFormExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? UsingName { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
