namespace QlParse;

public sealed class DropSequenceStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken SequenceKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Behavior { get; init; }
    public PgDropExtras? Postgres { get; init; }
}
