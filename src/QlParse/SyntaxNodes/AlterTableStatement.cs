namespace QlParse;

public sealed class AlterTableStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterTableAction Action { get; init; }
}
