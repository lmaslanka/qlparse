namespace QlParse;

public partial class SqlVisitor
{
    // Workstream B: SQL Server-only expression nodes (conversions, full-text predicates, AI functions,
    // AT TIME ZONE, NEXT VALUE FOR ... OVER), tried after the Postgres operators.
    internal bool TryVisitTsqlExpression(Expression expression)
    {
        switch (expression)
        {
            case TsqlConvertExpression node:
                VisitTsqlConvertExpression(node);
                return true;
            case TsqlCastExpression node:
                VisitTsqlCastExpression(node);
                return true;
            case TsqlFullTextPredicateExpression node:
                VisitTsqlFullTextPredicateExpression(node);
                return true;
            case TsqlGenerateEmbeddingsExpression node:
                VisitTsqlGenerateEmbeddingsExpression(node);
                return true;
            case TsqlAtTimeZoneExpression node:
                VisitTsqlAtTimeZoneExpression(node);
                return true;
            case TsqlNextValueOverExpression node:
                VisitTsqlNextValueOverExpression(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlConvertExpression(TsqlConvertExpression node)
    {
        VisitDataType(node.Type);
        Visit(node.Expression);
        if (node.Style is not null)
        {
            Visit(node.Style);
        }
    }

    public virtual void VisitTsqlCastExpression(TsqlCastExpression node)
    {
        Visit(node.Expression);
        VisitDataType(node.Type);
        if (node.Culture is not null)
        {
            Visit(node.Culture);
        }
    }

    public virtual void VisitTsqlFullTextPredicateExpression(TsqlFullTextPredicateExpression node)
    {
        Visit(node.Columns);
        Visit(node.SearchCondition);
        if (node.Language is not null)
        {
            Visit(node.Language);
        }
    }

    public virtual void VisitTsqlGenerateEmbeddingsExpression(TsqlGenerateEmbeddingsExpression node)
    {
        Visit(node.Source);
        if (node.Parameters is not null)
        {
            Visit(node.Parameters);
        }
    }

    public virtual void VisitTsqlAtTimeZoneExpression(TsqlAtTimeZoneExpression node)
    {
        Visit(node.Expression);
        Visit(node.TimeZone);
    }

    public virtual void VisitTsqlNextValueOverExpression(TsqlNextValueOverExpression node)
    {
        VisitNextValueExpression(node.NextValue);
        VisitWindowSpecification(node.Over);
    }
}
