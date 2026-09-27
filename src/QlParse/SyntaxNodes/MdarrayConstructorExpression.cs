namespace QlParse;

public sealed class MdarrayConstructorExpression : Expression
{
    public required SyntaxToken MdarrayKeyword { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public IReadOnlyList<MdarrayDimension> Dimensions { get; init; } = [];
    public SyntaxToken? CloseBracket { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Elements { get; init; } = [];
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
