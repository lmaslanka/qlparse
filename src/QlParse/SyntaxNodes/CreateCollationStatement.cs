namespace QlParse;

public sealed class CreateCollationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> CharacterSet { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Source { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? PadKeyword { get; init; }
    public SyntaxToken? SpaceKeyword { get; init; }
}
