namespace QlParse;

public sealed class DistinctFromExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken DistinctKeyword { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Right { get; init; }
}
