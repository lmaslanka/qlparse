namespace QlParse;

// `UPDATETEXT [BULK] table.column text_pointer {NULL | offset} {NULL | delete_length} [WITH LOG]
// [data | table.source_column source_text_pointer]`.
public sealed class TsqlUpdateTextStatement : Query
{
    public required SyntaxToken UpdateTextKeyword { get; init; }
    public SyntaxToken? BulkKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Column { get; init; }
    public required Expression TextPointer { get; init; }
    public required Expression InsertOffset { get; init; }
    public required Expression DeleteLength { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? LogKeyword { get; init; }
    public Expression? Data { get; init; }
    public Expression? SourceTextPointer { get; init; }
}
