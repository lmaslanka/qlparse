namespace QlParse;

// `REBUILD [PARTITION = {ALL|n}] [WITH (...)]`.
public sealed class TsqlRebuildAction : AlterTableAction
{
    public required SyntaxToken RebuildKeyword { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Partition { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
