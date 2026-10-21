namespace QlParse;

// `RECEIVE [TOP (n)] column, ... FROM queue [INTO @table] [WHERE {conversation_handle = @h |
// conversation_group_id = @g}]`; also the body of WAITFOR (RECEIVE ...).
public sealed class TsqlReceiveStatement : Query
{
    public required SyntaxToken ReceiveKeyword { get; init; }
    public SyntaxToken? TopKeyword { get; init; }
    public SyntaxToken? TopOpenParen { get; init; }
    public Expression? Top { get; init; }
    public SyntaxToken? TopCloseParen { get; init; }
    public required IReadOnlyList<TsqlReceiveColumn> Columns { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Queue { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public SyntaxToken? Into { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
}
