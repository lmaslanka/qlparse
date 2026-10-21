namespace QlParse;

internal sealed partial class Parser
{
    private Query ParseNonPgStatement() =>
        IsSqlServer ? TryParseTsqlStatement() ?? ParseStatementByKind() : ParseStatementByKind();

    // SQL Server-only statement dispatch, reached only when SqlOptions.SqlServer is set.
    // Every workstream owns one hook in its own Parser.TsqlStream file and returns null when the
    // current token does not start one of its statements, so the ISO dispatch runs instead.
    private Query? TryParseTsqlStatement() =>
        TryParseTsqlDmlStatement() ?? TryParseTsqlDdlStatement() ?? TryParseTsqlProcStatement() ?? TryParseTsqlSecurityStatement() ?? TryParseTsqlAdminStatement();
}
