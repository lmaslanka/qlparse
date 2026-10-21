namespace QlParse;

// `table [AS alias] WITH (hint, ...)`: table hints on a FROM item or a DML target.
public sealed class TsqlHintedTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlHint> Hints { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
