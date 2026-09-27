namespace QlParse;

public sealed class IsExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? UniqueWith { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? KeysKeyword { get; init; }
}
