namespace QlParse;

public sealed class QualifiedStarExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Star { get; init; }
}
