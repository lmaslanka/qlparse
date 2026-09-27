namespace QlParse;

public sealed class MdarraySliceExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<MdarrayAxis> Axes { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}
