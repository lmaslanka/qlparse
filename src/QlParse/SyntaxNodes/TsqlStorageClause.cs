namespace QlParse;

// `ON {filegroup | partition_scheme (column) | "default"}`, or TEXTIMAGE_ON / FILESTREAM_ON and their target.
public sealed class TsqlStorageClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Keyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Column { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
