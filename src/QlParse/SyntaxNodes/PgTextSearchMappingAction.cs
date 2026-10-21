namespace QlParse;

public sealed class PgTextSearchMappingAction : PgAlterAction
{
    public required SyntaxToken Action { get; init; }
    public required SyntaxToken MappingKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> TokenTypes { get; init; } = [];
    public SyntaxToken? ReplaceKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? OldDictionary { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Dictionaries { get; init; } = [];
}
