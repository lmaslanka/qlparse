namespace QlParse;

// `SWITCH [PARTITION n] TO target [PARTITION m] [WITH (...)]`.
public sealed class TsqlSwitchPartitionAction : AlterTableAction
{
    public required SyntaxToken SwitchKeyword { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public Expression? SourcePartition { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Target { get; init; }
    public SyntaxToken? TargetPartitionKeyword { get; init; }
    public Expression? TargetPartition { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
