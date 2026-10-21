namespace QlParse;

// `CREATE QUEUE [db.][schema.]name [WITH option, ...] [ON {filegroup | [DEFAULT]}]`.
public sealed class TsqlCreateQueueStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken QueueKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? FileGroup { get; init; }
}
