namespace QlParse;

// SQL Server `DROP {TABLE|VIEW|PROCEDURE|FUNCTION|TRIGGER|SEQUENCE|SYNONYM|TYPE|PARTITION FUNCTION|
// PARTITION SCHEME|STATISTICS} [IF EXISTS] name, ...`; DDL triggers add `ON {DATABASE|ALL SERVER}`. The Service
// Broker, server, full-text and CLR kinds (QUEUE, MESSAGE TYPE, EVENT SESSION, FULLTEXT CATALOG, AGGREGATE, ...)
// use the same shape: EVENT SESSION and EVENT NOTIFICATION put `SERVER`, `DATABASE` or `QUEUE` and the queue's
// name parts in Scope, and DROP FULLTEXT INDEX ON table has no Names and puts the table's name parts in Scope.
public sealed class TsqlDropObjectsStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required IReadOnlyList<IReadOnlyList<SyntaxToken>> Names { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Scope { get; init; } = [];
}
