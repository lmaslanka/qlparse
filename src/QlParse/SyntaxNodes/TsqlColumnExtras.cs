namespace QlParse;

// SQL Server-only parts of a column definition (computed `AS expr [PERSISTED]`, IDENTITY, FILESTREAM, SPARSE,
// ROWGUIDCOL, HIDDEN, COLUMN_SET FOR ALL_SPARSE_COLUMNS, MASKED WITH, ENCRYPTED WITH, the name of an inline
// DEFAULT constraint and its WITH VALUES, an inline column index); allocated only when at least one is present.
// A computed column has no data type.
public sealed class TsqlColumnExtras
{
    public TsqlComputedColumn? Computed { get; init; }
    public TsqlIdentitySpec? Identity { get; init; }
    public SyntaxToken? FilestreamKeyword { get; init; }
    public SyntaxToken? SparseKeyword { get; init; }
    public SyntaxToken? RowguidcolKeyword { get; init; }
    public SyntaxToken? HiddenKeyword { get; init; }
    public SyntaxToken? ColumnSetKeyword { get; init; }
    public SyntaxToken? ColumnSetForKeyword { get; init; }
    public SyntaxToken? AllSparseColumnsKeyword { get; init; }
    public TsqlMaskingClause? Masking { get; init; }
    public TsqlEncryptionClause? Encryption { get; init; }
    public SyntaxToken? DefaultConstraintKeyword { get; init; }
    public SyntaxToken? DefaultConstraintName { get; init; }
    public SyntaxToken? DefaultWithKeyword { get; init; }
    public SyntaxToken? DefaultValuesKeyword { get; init; }
    public TsqlTableIndex? Index { get; init; }
}
