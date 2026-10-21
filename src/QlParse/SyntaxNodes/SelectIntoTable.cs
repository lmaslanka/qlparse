namespace QlParse;

public sealed class SelectIntoTable
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Modifier { get; init; }
    public SyntaxToken? TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
}
