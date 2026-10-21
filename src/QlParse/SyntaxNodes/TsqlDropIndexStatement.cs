namespace QlParse;

// `DROP INDEX [IF EXISTS] {name ON table [WITH (...)] | table.name}, ...`.
public sealed class TsqlDropIndexStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required IReadOnlyList<TsqlDropIndexItem> Items { get; init; }
}
