namespace QlParse;

// Postgres-only parts of ALTER COLUMN (`TYPE ... COLLATE ... USING`, SET/RESET attributes and
// storage parameters, ADD GENERATED ... AS IDENTITY, DROP EXPRESSION, DROP IDENTITY IF EXISTS, OPTIONS);
// allocated only when at least one is present.
public sealed class PgAlterColumnExtras
{
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public Expression? Using { get; init; }
    public SyntaxToken? AttributeKeyword { get; init; }
    public Expression? AttributeValue { get; init; }
    public SyntaxToken? AttributeName { get; init; }
    public SyntaxToken? ResetKeyword { get; init; }
    public StorageParameterList? Parameters { get; init; }
    public IdentityColumn? Identity { get; init; }
    public SyntaxToken? ExpressionKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }

    // Postgres `ALTER [COLUMN] c OPTIONS ([ADD | SET | DROP] option ['value'] [, ...])` (foreign tables).
    public PgOptionList? Options { get; init; }
}
