namespace QlParse;

public partial class SqlVisitor
{
    // Workstream G: SQL Server-only administration, Service Broker and full-text statement nodes.
    private static bool TryVisitTsqlAdminQuery(Query query) => query is null;
}
