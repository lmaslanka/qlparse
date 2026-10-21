namespace QlParse;

// `ALTER COLUMN name type [COLLATE c] [NULL|NOT NULL] [SPARSE] [ENCRYPTED WITH (...)]` or
// `ALTER COLUMN name {ADD|DROP} {ROWGUIDCOL|PERSISTED|NOT FOR REPLICATION|SPARSE|HIDDEN|MASKED [WITH (...)]}`,
// optionally followed by `WITH (ONLINE = ON)`.
public sealed class TsqlAlterColumnAction : AlterTableAction
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ColumnKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public DataType? DataType { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? SparseKeyword { get; init; }
    public TsqlEncryptionClause? Encryption { get; init; }
    public SyntaxToken? VerbKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Property { get; init; } = [];
    public TsqlMaskingClause? Masking { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
