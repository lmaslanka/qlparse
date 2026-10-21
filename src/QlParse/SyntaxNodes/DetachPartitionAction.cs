namespace QlParse;

public sealed class DetachPartitionAction : AlterTableAction
{
    public required SyntaxToken DetachKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Mode { get; init; }
}
