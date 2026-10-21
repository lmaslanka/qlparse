namespace QlParse;

public sealed class PlpgsqlSqlStatement : Query
{
    public required Query Statement { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public SyntaxToken? StrictKeyword { get; init; }
    public IReadOnlyList<Expression> Targets { get; init; } = [];
}
