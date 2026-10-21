namespace QlParse;

// Keys, certificates, credentials and server audits: a verb and object kind (Keywords, e.g. `CREATE SYMMETRIC KEY`,
// `OPEN MASTER KEY`, `CLOSE ALL SYMMETRIC KEYS`, `CREATE DATABASE SCOPED CREDENTIAL`, `ALTER SERVER AUDIT`,
// `BACKUP CERTIFICATE`, `RESTORE SERVICE MASTER KEY`), an optional name and
// a sequence of clauses such as `AUTHORIZATION u`, `FROM FILE = '...'`, `WITH SUBJECT = '...'`, `ENCRYPTION BY PASSWORD = '...'`,
// `TO FILE (...)`, plus a server audit's `WHERE predicate`.
public sealed class TsqlSecurityObjectStatement : Query
{
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? Name { get; init; }
    public IReadOnlyList<TsqlObjectOption> Clauses { get; init; } = [];
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
}
