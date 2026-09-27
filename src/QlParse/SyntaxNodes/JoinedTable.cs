namespace QlParse;

public sealed class JoinedTable : TableSource
{
    public required SyntaxToken OpenParen { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
