namespace QlParse;

public partial class SqlVisitor
{
    // Workstream A: SQL Server scripts and batches.
    internal bool TryVisitTsqlLexicalQuery(Query query)
    {
        switch (query)
        {
            case TsqlScript node:
                VisitTsqlScript(node);
                return true;
            case TsqlBatch node:
                VisitTsqlBatch(node);
                return true;
            default:
                return false;
        }
    }

    // Workstream A: SQL Server lexical expressions (@@system functions, $PARTITION.fn(expr)).
    internal bool TryVisitTsqlLexicalExpression(Expression expression)
    {
        switch (expression)
        {
            case TsqlGlobalVariableExpression node:
                VisitTsqlGlobalVariableExpression(node);
                return true;
            case TsqlPartitionFunctionCallExpression node:
                VisitTsqlPartitionFunctionCallExpression(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlScript(TsqlScript node)
    {
        foreach (var batch in node.Batches)
        {
            VisitTsqlBatch(batch);
        }
    }

    public virtual void VisitTsqlBatch(TsqlBatch node)
    {
        foreach (var statement in node.Statements)
        {
            Visit(statement);
        }

        if (node.Separator is not null)
        {
            VisitTsqlBatchSeparator(node.Separator);
        }
    }

    public virtual void VisitTsqlBatchSeparator(TsqlBatchSeparator node)
    {
        return;
    }

    public virtual void VisitTsqlGlobalVariableExpression(TsqlGlobalVariableExpression node)
    {
        return;
    }

    public virtual void VisitTsqlPartitionFunctionCallExpression(TsqlPartitionFunctionCallExpression node) =>
        Visit(node.Argument);
}
