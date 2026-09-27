namespace QlParse;

public sealed class InsertStatement : Query
{
    public required SyntaxToken InsertKeyword { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public OverrideClause? Override { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public Query? Query { get; init; }
}
