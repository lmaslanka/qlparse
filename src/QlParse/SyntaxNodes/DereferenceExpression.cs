namespace QlParse;

public sealed class DereferenceExpression : Expression
{
    public required Expression Reference { get; init; }
    public required SyntaxToken Arrow { get; init; }
    public required SyntaxToken Attribute { get; init; }
}
