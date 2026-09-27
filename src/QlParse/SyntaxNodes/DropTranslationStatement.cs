namespace QlParse;

public sealed class DropTranslationStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TranslationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}
