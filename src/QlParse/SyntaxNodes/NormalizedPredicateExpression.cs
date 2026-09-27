namespace QlParse;

public sealed class NormalizedPredicateExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? Form { get; init; }
    public required SyntaxToken NormalizedKeyword { get; init; }
}
