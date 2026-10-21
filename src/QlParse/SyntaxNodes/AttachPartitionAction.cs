namespace QlParse;

public sealed class AttachPartitionAction : AlterTableAction
{
    public required SyntaxToken AttachKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public PartitionBound? Bound { get; init; }
}
