namespace QlParse;

// `WRITETEXT [BULK] table.column text_pointer [WITH LOG] data`.
public sealed class TsqlWriteTextStatement : Query
{
    public required SyntaxToken WriteTextKeyword { get; init; }
    public SyntaxToken? BulkKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Column { get; init; }
    public required Expression TextPointer { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? LogKeyword { get; init; }
    public required Expression Data { get; init; }
}
