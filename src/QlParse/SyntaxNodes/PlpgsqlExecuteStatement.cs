namespace QlParse;

public sealed class PlpgsqlExecuteStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required Expression Command { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public SyntaxToken? StrictKeyword { get; init; }
    public IReadOnlyList<Expression> Targets { get; init; } = [];
    public SyntaxToken? UsingKeyword { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
}
