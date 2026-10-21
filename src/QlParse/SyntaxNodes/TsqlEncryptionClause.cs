namespace QlParse;

// Always Encrypted column option: `ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = k, ENCRYPTION_TYPE = ..., ALGORITHM = '...')`.
public sealed class TsqlEncryptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken EncryptedKeyword { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required TsqlRelationalOptionList Options { get; init; }
}
