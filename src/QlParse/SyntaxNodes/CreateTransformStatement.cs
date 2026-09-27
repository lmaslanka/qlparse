namespace QlParse;

public sealed class CreateTransformStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TransformKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required IReadOnlyList<TransformGroup> Groups { get; init; }
}
