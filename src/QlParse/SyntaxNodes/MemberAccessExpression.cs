namespace QlParse;

public sealed class MemberAccessExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Member { get; init; }
}
