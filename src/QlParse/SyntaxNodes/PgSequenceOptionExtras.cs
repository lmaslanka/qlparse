namespace QlParse;

// Postgres sequence options with a name target (`SEQUENCE NAME n`, `OWNED BY t.c`);
// allocated only when at least one is present.
public sealed class PgSequenceOptionExtras
{
    public SyntaxToken? NameKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Target { get; init; }
}
