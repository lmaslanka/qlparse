namespace QlParse;

// `ALTER INDEX {name|ALL} ON table {REBUILD|REORGANIZE|DISABLE|RESUME|PAUSE|ABORT|SET (...)}
// [PARTITION = {ALL|n}] [WITH (...)]`.
public sealed class TsqlAlterIndexStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
    public SyntaxToken? AllKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken ActionKeyword { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Partition { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
