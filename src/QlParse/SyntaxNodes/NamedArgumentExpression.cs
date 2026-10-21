namespace QlParse;

public sealed class NamedArgumentExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Arrow { get; init; }
    public required Expression Value { get; init; }
}
