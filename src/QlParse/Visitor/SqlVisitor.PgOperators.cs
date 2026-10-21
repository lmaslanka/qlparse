namespace QlParse;

public partial class SqlVisitor
{
    // Postgres expression nodes; dispatched after every ISO bucket so ISO nodes keep their dispatch cost.
    private bool TryVisitPgOperatorExpression(Expression expression)
    {
        switch (expression)
        {
            case PositionalParameterExpression node:
                VisitPositionalParameterExpression(node);
                return true;
            case NamedArgumentExpression node:
                VisitNamedArgumentExpression(node);
                return true;
            case VariadicArgumentExpression node:
                VisitVariadicArgumentExpression(node);
                return true;
            case QualifiedOperatorExpression node:
                VisitQualifiedOperatorExpression(node);
                return true;
            case QuantifiedArrayExpression node:
                VisitQuantifiedArrayExpression(node);
                return true;
            case AggregateCallExpression node:
                VisitAggregateCallExpression(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitPositionalParameterExpression(PositionalParameterExpression node)
    {
        return;
    }

    public virtual void VisitNamedArgumentExpression(NamedArgumentExpression node) => Visit(node.Value);

    public virtual void VisitVariadicArgumentExpression(VariadicArgumentExpression node) => Visit(node.Argument);

    public virtual void VisitQualifiedOperatorExpression(QualifiedOperatorExpression node)
    {
        if (node.Left is not null)
        {
            Visit(node.Left);
        }

        Visit(node.Right);
    }

    public virtual void VisitQuantifiedArrayExpression(QuantifiedArrayExpression node)
    {
        Visit(node.Left);
        Visit(node.Array);
    }

    public virtual void VisitAggregateCallExpression(AggregateCallExpression node)
    {
        VisitAll(node.Arguments);
        if (node.OrderBy is not null)
        {
            VisitOrderByClause(node.OrderBy);
        }

        if (node.WithinGroup is not null)
        {
            VisitWithinGroupClause(node.WithinGroup);
        }

        if (node.Filter is not null)
        {
            VisitFilterClause(node.Filter);
        }

        if (node.Over is not null)
        {
            VisitWindowSpecification(node.Over);
        }
    }

    public virtual void VisitWithinGroupClause(WithinGroupClause node) => VisitOrderByClause(node.OrderBy);
}
