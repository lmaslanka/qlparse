namespace QlParse;

public sealed class CommentStatement : Query
{
    public required SyntaxToken CommentKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public IReadOnlyList<SyntaxToken> ExtraKindKeywords { get; init; } = [];
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<DataType> ArgumentTypes { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? TargetOnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
