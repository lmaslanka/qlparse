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

    // SQL Server-only expression starts, reached from the prefix parser's fallback arm after the Postgres
    // hook, so ISO prefix dispatch is unchanged. Workstreams chain their own hook here.
    internal Expression? TryParseTsqlPrefix() => IsSqlServer ? TryParseTsqlLexicalPrefix() ?? TryParseTsqlExpressionPrefix() : null;
}
