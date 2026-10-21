namespace QlParse;

// SQL Server `DROP {TABLE|VIEW|PROCEDURE|FUNCTION|TRIGGER|SEQUENCE|SYNONYM|TYPE|PARTITION FUNCTION|
// PARTITION SCHEME|STATISTICS} [IF EXISTS] name, ...`; DDL triggers add `ON {DATABASE|ALL SERVER}`.
public sealed class TsqlDropObjectsStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required IReadOnlyList<IReadOnlyList<SyntaxToken>> Names { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Scope { get; init; } = [];
}
