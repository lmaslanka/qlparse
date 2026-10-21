namespace QlParse;

// `CREATE FULLTEXT INDEX ON table [(column, ...)] KEY INDEX index [ON {catalog | (catalog, FILEGROUP fg)}]
// [WITH [(] option, ... [)]]`.
public sealed class TsqlCreateFulltextIndexStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlFulltextIndexColumn> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken KeyKeyword { get; init; }
    public required SyntaxToken KeyIndexKeyword { get; init; }
    public required SyntaxToken KeyIndex { get; init; }
    public SyntaxToken? CatalogOnKeyword { get; init; }
    public TsqlOptionList? Storage { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
