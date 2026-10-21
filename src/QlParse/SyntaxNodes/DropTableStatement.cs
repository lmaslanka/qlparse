namespace QlParse;

public sealed class DropTableStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Behavior { get; init; }
    public PgDropExtras? Postgres { get; init; }
}
