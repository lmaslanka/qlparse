namespace QlParse;

public sealed class ExecutePreparedStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
