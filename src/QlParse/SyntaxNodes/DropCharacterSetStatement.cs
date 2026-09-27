namespace QlParse;

public sealed class DropCharacterSetStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}
