namespace QlParse;

// Postgres-only parts of CREATE FUNCTION / PROCEDURE (OR REPLACE, RETURNS SETOF / %TYPE / TABLE, AS definition);
// allocated only when at least one is present.
public sealed class PgCreateRoutineExtras
{
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public SyntaxToken? SetofKeyword { get; init; }
    public PgTypeAttribute? ReturnsTypeAttribute { get; init; }
    public PgReturnsTable? ReturnsTable { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Definition { get; init; }
    public SyntaxToken? LinkSymbol { get; init; }
    public IReadOnlyList<SyntaxToken> BodyTokens { get; init; } = [];
    public IReadOnlyList<SyntaxTrivia> BodyTrivia { get; init; } = [];
}
