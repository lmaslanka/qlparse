namespace QlParse;

public sealed class ClusterAction : AlterTableAction
{
    public SyntaxToken? SetKeyword { get; init; }
    public SyntaxToken? WithoutKeyword { get; init; }
    public required SyntaxToken ClusterKeyword { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? Index { get; init; }
}
