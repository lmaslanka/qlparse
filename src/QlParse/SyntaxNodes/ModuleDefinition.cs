namespace QlParse;

public sealed class ModuleDefinition : Query
{
    public required SyntaxToken ModuleKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? NamesKeyword { get; init; }
    public SyntaxToken? AreKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? CharacterSet { get; init; }
    public required SyntaxToken LanguageKeyword { get; init; }
    public required SyntaxToken Language { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? SchemaName { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Authorization { get; init; }
    public SyntaxToken? PathKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Path { get; init; } = [];
    public IReadOnlyList<Query> Contents { get; init; } = [];
}
