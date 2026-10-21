namespace QlParse;

// Nodes added while closing gaps in the Postgres workstreams.
public partial class SqlVisitor
{
    public virtual void VisitOrderedQuery(OrderedQuery node)
    {
        Visit(node.Query);
        if (node.OrderBy is not null)
        {
            VisitOrderByClause(node.OrderBy);
        }

        if (node.Limit is not null)
        {
            VisitLimitClause(node.Limit);
        }

        if (node.Offset is not null)
        {
            VisitOffsetClause(node.Offset);
        }

        if (node.Fetch is not null)
        {
            VisitFetchClause(node.Fetch);
        }

        if (node.Lock is not null)
        {
            VisitLockClause(node.Lock);
        }
    }

    public virtual void VisitLateralTable(LateralTable node) => Visit(node.Table);

    public virtual void VisitMergeDoNothingAction(MergeDoNothingAction node)
    {
        return;
    }

    private bool TryVisitPgGapAlterTableAction(AlterTableAction action)
    {
        switch (action)
        {
            case AlterConstraintAction node:
                VisitAlterConstraintAction(node);
                return true;
            case SetAccessMethodAction node:
                VisitSetAccessMethodAction(node);
                return true;
            case SetWithoutOidsAction node:
                VisitSetWithoutOidsAction(node);
                return true;
            case TypedTableAction node:
                VisitTypedTableAction(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitAlterConstraintAction(AlterConstraintAction node)
    {
        return;
    }

    public virtual void VisitSetAccessMethodAction(SetAccessMethodAction node)
    {
        return;
    }

    public virtual void VisitSetWithoutOidsAction(SetWithoutOidsAction node)
    {
        return;
    }

    public virtual void VisitTypedTableAction(TypedTableAction node)
    {
        return;
    }

    public virtual void VisitPgForeignTableAction(PgForeignTableAction node) => Visit(node.Action);

    public virtual void VisitPgDomainAction(PgDomainAction node)
    {
        return;
    }

    private void VisitTableConstraints(IReadOnlyList<TableConstraint> constraints)
    {
        foreach (var constraint in constraints)
        {
            VisitTableConstraint(constraint);
        }
    }
}
