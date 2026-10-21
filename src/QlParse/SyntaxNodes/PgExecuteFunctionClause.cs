namespace QlParse;

public sealed class PgExecuteFunctionClause : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken FunctionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
