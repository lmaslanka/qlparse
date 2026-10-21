namespace QlParse;

// `WITH (column type ['path' | ordinal] [AS JSON], ...)` after OPENJSON / OPENXML / OPENROWSET(BULK ...), or OPENXML's
// `WITH table_name`.
public sealed class TsqlRowsetSchema
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlRowsetColumn> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public IReadOnlyList<SyntaxToken> TableName { get; init; } = [];
}
