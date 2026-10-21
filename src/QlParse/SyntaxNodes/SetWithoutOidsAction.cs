namespace QlParse;

// Postgres `ALTER TABLE ... SET WITHOUT OIDS` (accepted for compatibility; a no-op since PG 12).
public sealed class SetWithoutOidsAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken WithoutKeyword { get; init; }
    public required SyntaxToken OidsKeyword { get; init; }
}
