namespace QlParse;

public sealed class ArrayExpression : Expression
{
    public required SyntaxToken ArrayKeyword { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}
