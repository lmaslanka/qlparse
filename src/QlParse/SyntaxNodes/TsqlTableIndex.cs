namespace QlParse;

// Inline `INDEX name [UNIQUE] [CLUSTERED|NONCLUSTERED] [COLUMNSTORE|HASH] (columns) ...` in CREATE TABLE,
// CREATE TYPE ... AS TABLE or ALTER TABLE ... ADD; a column-level index has no column list.
public sealed class TsqlTableIndex : TableElement
{
    public required SyntaxToken IndexKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? ClusteredKeyword { get; init; }
    public SyntaxToken? ColumnstoreKeyword { get; init; }
    public SyntaxToken? HashKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<IndexColumn>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public TsqlIncludeClause? Include { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
    public TsqlStorageClause? Storage { get; init; }
    public TsqlStorageClause? FilestreamOn { get; init; }
}
