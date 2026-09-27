namespace QlParse;

public sealed class IntervalLiteralExpression : Expression
{
    public required SyntaxToken IntervalKeyword { get; init; }
    public SyntaxToken? Sign { get; init; }
    public required SyntaxToken Literal { get; init; }
    public required IntervalQualifier Qualifier { get; init; }
}
