namespace QlParse;

public partial class SqlVisitor
{
    // Workstream C: SQL Server-only query and DML statement nodes.
    private static bool TryVisitTsqlDmlQuery(Query query) => query is null;
}
