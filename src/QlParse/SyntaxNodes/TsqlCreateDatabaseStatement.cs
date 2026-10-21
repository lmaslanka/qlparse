namespace QlParse;

// CREATE DATABASE name [CONTAINMENT = ...] [ON [PRIMARY] file, ... [, FILEGROUP ...]] [LOG ON file, ...]
// [COLLATE c] [WITH option, ...] [FOR {ATTACH [WITH ...] | ATTACH_REBUILD_LOG}] [AS SNAPSHOT OF source].
public sealed class TsqlCreateDatabaseStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public TsqlObjectOption? Containment { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<TsqlDatabaseFilegroup> Filegroups { get; init; } = [];
    public SyntaxToken? LogKeyword { get; init; }
    public SyntaxToken? LogOnKeyword { get; init; }
    public IReadOnlyList<TsqlObjectOptionList> LogFiles { get; init; } = [];
    public SyntaxToken? CollateKeyword { get; init; }
    public SyntaxToken? Collation { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? AttachKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlObjectOption> Options { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? SnapshotKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public SyntaxToken? Source { get; init; }
}
