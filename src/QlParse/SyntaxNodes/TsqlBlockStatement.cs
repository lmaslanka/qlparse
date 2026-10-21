namespace QlParse;

// BEGIN statements END.
public sealed class TsqlBlockStatement : Query
{
    public required SyntaxToken BeginKeyword { get; init; }
    public required IReadOnlyList<Query> Statements { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
}
