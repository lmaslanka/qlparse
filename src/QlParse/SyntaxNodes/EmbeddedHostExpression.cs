namespace QlParse;

public sealed class EmbeddedHostExpression : Expression
{
    public required SyntaxToken Name { get; init; }
}
