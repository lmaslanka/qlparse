namespace QlParse;

// `ADD element, element, ...`: several columns, constraints, indexes or periods after one ADD (a single
// element keeps the ISO AddColumnAction / AddConstraintAction shape).
public sealed class TsqlAddElementsAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required IReadOnlyList<TableElement> Elements { get; init; }
}
