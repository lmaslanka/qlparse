namespace QlParse;

public sealed class SetCatalogStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CatalogKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}
