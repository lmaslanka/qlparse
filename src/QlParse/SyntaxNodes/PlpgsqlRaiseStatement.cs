namespace QlParse;

public sealed class PlpgsqlRaiseStatement : Query
{
    public required SyntaxToken RaiseKeyword { get; init; }
    public SyntaxToken? Level { get; init; }
    public SyntaxToken? Format { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? Condition { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public IReadOnlyList<PlpgsqlRaiseOption> Options { get; init; } = [];
}
