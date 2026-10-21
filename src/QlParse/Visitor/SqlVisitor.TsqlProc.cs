namespace QlParse;

public partial class SqlVisitor
{
    // Workstream E: SQL Server-only procedural, trigger, transaction and session statement nodes.
    private static bool TryVisitTsqlProcQuery(Query query) => query is null;
}
