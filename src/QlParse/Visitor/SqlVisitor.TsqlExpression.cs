namespace QlParse;

public partial class SqlVisitor
{
    // Workstream B: SQL Server-only expression nodes (types, functions, methods), tried after the Postgres operators.
    private static bool TryVisitTsqlExpression(Expression expression) => expression is null;
}
