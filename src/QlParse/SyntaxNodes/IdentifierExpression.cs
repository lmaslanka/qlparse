namespace QlParse;

public sealed class IdentifierExpression : Expression
{
    public required SyntaxToken Identifier { get; init; }
}
