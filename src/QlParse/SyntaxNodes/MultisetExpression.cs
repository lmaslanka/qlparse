namespace QlParse;

public sealed class MultisetExpression : Expression
{
    public required SyntaxToken MultisetKeyword { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}
