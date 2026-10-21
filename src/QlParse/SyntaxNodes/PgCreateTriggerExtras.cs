namespace QlParse;

// Postgres-only parts of CREATE TRIGGER (OR REPLACE, CONSTRAINT, `OR` events, FROM table, deferrability);
// allocated only when at least one is present.
public sealed class PgCreateTriggerExtras
{
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public IReadOnlyList<PgTriggerEvent> AdditionalEvents { get; init; } = [];
    public SyntaxToken? FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ReferencedTable { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}
