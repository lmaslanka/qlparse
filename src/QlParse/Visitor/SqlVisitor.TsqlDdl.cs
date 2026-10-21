namespace QlParse;

public partial class SqlVisitor
{
    // Workstream D: SQL Server-only DDL statement nodes.
    private static bool TryVisitTsqlDdlQuery(Query query) => query is null;
}
