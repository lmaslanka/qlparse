namespace QlParse;

// One filegroup of CREATE DATABASE ... ON: the leading `[PRIMARY] file, ...` group (no FILEGROUP keyword) or
// `FILEGROUP name [CONTAINS {FILESTREAM | MEMORY_OPTIMIZED_DATA}] [DEFAULT] file, ...`. Each file is its `(NAME = ..., ...)` list.
public sealed class TsqlDatabaseFilegroup
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? PrimaryKeyword { get; init; }
    public SyntaxToken? FilegroupKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? ContainsKeyword { get; init; }
    public SyntaxToken? ContainsKind { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public IReadOnlyList<TsqlObjectOptionList> Files { get; init; } = [];
}
