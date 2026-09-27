namespace QlParse;

public sealed class CommentStatement : Query
{
    public required SyntaxToken CommentKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
