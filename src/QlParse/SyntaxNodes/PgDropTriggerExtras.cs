namespace QlParse;

// Postgres-only parts of DROP TRIGGER (IF EXISTS, `ON table`, CASCADE / RESTRICT);
// allocated only when at least one is present.
public sealed class PgDropTriggerExtras
{
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
