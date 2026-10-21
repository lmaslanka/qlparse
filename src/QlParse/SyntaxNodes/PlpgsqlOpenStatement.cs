namespace QlParse;

public sealed class PlpgsqlOpenStatement : Query
{
    public required SyntaxToken OpenKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ScrollKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public Query? Query { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
