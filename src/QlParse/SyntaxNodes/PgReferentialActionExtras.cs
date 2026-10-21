namespace QlParse;

// Postgres `SET NULL (columns)` / `SET DEFAULT (columns)`;
// allocated only when at least one is present.
public sealed class PgReferentialActionExtras
{
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
}
