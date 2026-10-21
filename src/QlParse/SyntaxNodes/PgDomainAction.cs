namespace QlParse;

// Postgres-only ALTER DOMAIN actions:
//   SET NOT NULL / DROP NOT NULL          Keywords only
//   SET SCHEMA s                          Keywords SET SCHEMA, Target s
//   VALIDATE CONSTRAINT c                 Keywords VALIDATE CONSTRAINT, Target c
//   RENAME CONSTRAINT a TO b              Keywords RENAME CONSTRAINT, Target a, ToKeyword, NewName b
//   RENAME TO n / OWNER TO r              Keywords RENAME / OWNER, ToKeyword, NewName
public sealed class PgDomainAction : AlterDomainAction
{
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? Target { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? NewName { get; init; }
}
