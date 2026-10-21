namespace QlParse;

// `ALTER QUEUE name {WITH option, ... | REBUILD [WITH (option, ...)] | REORGANIZE [WITH (option, ...)]
// | MOVE TO filegroup}`: Action holds REBUILD, REORGANIZE or MOVE TO and is empty for the WITH form.
public sealed class TsqlAlterQueueStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken QueueKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public IReadOnlyList<SyntaxToken> Action { get; init; } = [];
    public SyntaxToken? FileGroup { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
