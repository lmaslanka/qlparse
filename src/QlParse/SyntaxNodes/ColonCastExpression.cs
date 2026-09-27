namespace QlParse;

public sealed class ColonCastExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken DoubleColon { get; init; }
    public required DataType Type { get; init; }
}
