namespace QlParse;

public sealed class SystemVersioningAction : AlterTableAction
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken SystemKeyword { get; init; }
    public required SyntaxToken VersioningKeyword { get; init; }
}
