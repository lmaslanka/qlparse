namespace QlParse;

internal sealed partial class Parser
{
    // Postgres-only statement dispatch, reached only when SqlOptions.Postgres is set.
    // Every workstream owns one hook in its own Parser.PgStream file and returns null when the
    // current token does not start one of its statements, so the ISO dispatch runs instead.
    private Query? TryParsePgStatement() =>
        TryParsePgDmlStatement() ?? TryParsePgDdlStatement() ?? TryParsePgSchemaStatement() ?? TryParsePgUtilityStatement() ?? TryParsePgRoutineStatement();
}
