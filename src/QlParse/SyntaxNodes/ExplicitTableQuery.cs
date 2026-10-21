namespace QlParse;

public sealed class ExplicitTableQuery : Query
{
    public required SyntaxToken TableKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? DescendantStar { get; init; }
}
