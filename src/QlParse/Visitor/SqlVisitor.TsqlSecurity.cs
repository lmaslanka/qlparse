namespace QlParse;

public partial class SqlVisitor
{
    // Workstream F: SQL Server-only database, object and security statement nodes.
    private static bool TryVisitTsqlSecurityQuery(Query query) => query is null;
}
