namespace QlParse;

// SQL Server INSERT: `INSERT [TOP (n) [PERCENT]] [INTO] target [WITH (hints)] [(columns)] [OUTPUT ...]
// {VALUES ... | query | DEFAULT VALUES} [OPTION (...)]`. Target is a TableReference (a name or @table variable),
// a TsqlRowsetFunctionTable, or a TsqlHintedTable around one.
public sealed class TsqlInsertStatement : Query
{
    public required SyntaxToken InsertKeyword { get; init; }
    public TsqlTopClause? Top { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public required TableSource Target { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public IReadOnlyList<TsqlOutputClause> Output { get; init; } = [];
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public Query? Source { get; init; }
    public TsqlOptionClause? Option { get; init; }
}
