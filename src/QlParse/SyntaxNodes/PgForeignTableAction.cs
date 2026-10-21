namespace QlParse;

// An ALTER TABLE-style action of `ALTER FOREIGN TABLE`: ADD / DROP / ALTER COLUMN, ADD / DROP CONSTRAINT, ...
public sealed class PgForeignTableAction : PgAlterAction
{
    public required AlterTableAction Action { get; init; }
}
