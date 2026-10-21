namespace QlParse;

public sealed class VariadicArgumentExpression : Expression
{
    public required SyntaxToken VariadicKeyword { get; init; }
    public required Expression Argument { get; init; }
}
