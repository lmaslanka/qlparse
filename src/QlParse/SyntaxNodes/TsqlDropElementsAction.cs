namespace QlParse;

// `DROP [CONSTRAINT|COLUMN] [IF EXISTS] name [WITH (...)], ...`; a name without its own kind keyword keeps the
// kind of the previous item (CONSTRAINT for the first one).
public sealed class TsqlDropElementsAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required IReadOnlyList<TsqlDropElement> Elements { get; init; }
}
