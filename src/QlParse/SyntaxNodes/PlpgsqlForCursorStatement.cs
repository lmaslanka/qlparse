namespace QlParse;

public sealed class PlpgsqlForCursorStatement : Query
{
    public PlpgsqlLabel? Label { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required PlpgsqlLoopBody Body { get; init; }
}
