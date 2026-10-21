namespace QlParse;

public sealed class SetPersistenceAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken Persistence { get; init; }
}
