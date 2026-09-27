namespace QlParse;

public sealed class SchemaDefinition : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Authorization { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? CharacterKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? CharacterSet { get; init; }
    public SyntaxToken? PathKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Path { get; init; }
    public IReadOnlyList<CreateTableStatement> Elements { get; init; } = [];
}
