namespace QlParse;

// Postgres `[NOT] DEFERRABLE` transaction mode;
// allocated only when at least one is present.
public sealed class PgTransactionModeExtras
{
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
}
