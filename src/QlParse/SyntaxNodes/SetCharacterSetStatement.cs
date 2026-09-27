namespace QlParse;

public sealed class SetCharacterSetStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken CharacterSetKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}
