namespace QlParse;

public sealed class InheritAction : AlterTableAction
{
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken InheritKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Parent { get; init; }
}
