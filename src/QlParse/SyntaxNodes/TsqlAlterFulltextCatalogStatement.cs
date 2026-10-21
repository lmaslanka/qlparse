namespace QlParse;

// `ALTER FULLTEXT CATALOG name {REBUILD [WITH ACCENT_SENSITIVITY = {ON | OFF}] | REORGANIZE | AS DEFAULT}`.
public sealed class TsqlAlterFulltextCatalogStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken CatalogKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required IReadOnlyList<SyntaxToken> Action { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
