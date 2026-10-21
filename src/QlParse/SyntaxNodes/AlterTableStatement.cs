namespace QlParse;

public sealed class AlterTableStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterTableAction Action { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public AllInTablespaceClause? AllInTablespace { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? Star { get; init; }
    public IReadOnlyList<AlterTableAction> AdditionalActions { get; init; } = [];
}
