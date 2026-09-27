namespace QlParse;

public sealed class CreateCharacterSetStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken GetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Source { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}
