namespace QlParse;

public sealed class DatetimeLiteralExpression : Expression
{
    public required SyntaxToken KindKeyword { get; init; }
    public required SyntaxToken Literal { get; init; }
}
