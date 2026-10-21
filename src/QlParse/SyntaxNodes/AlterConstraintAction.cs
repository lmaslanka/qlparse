namespace QlParse;

// Postgres `ALTER TABLE ... ALTER CONSTRAINT name [[NOT] DEFERRABLE] [INITIALLY {DEFERRED | IMMEDIATE}]`.
public sealed class AlterConstraintAction : AlterTableAction
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}
