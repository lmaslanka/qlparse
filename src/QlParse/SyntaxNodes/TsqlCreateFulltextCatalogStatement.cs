namespace QlParse;

// `CREATE FULLTEXT CATALOG name [ON FILEGROUP fg] [IN PATH 'path'] [WITH ACCENT_SENSITIVITY = {ON | OFF}]
// [AS DEFAULT] [AUTHORIZATION owner]`.
public sealed class TsqlCreateFulltextCatalogStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken CatalogKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? FileGroupKeyword { get; init; }
    public SyntaxToken? FileGroup { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? PathKeyword { get; init; }
    public SyntaxToken? Path { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
}
