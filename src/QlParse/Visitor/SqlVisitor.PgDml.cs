namespace QlParse;

// Postgres query-expression and DML nodes (workstream C).
public partial class SqlVisitor
{
    internal bool TryVisitQueryPgDml(Query query)
    {
        switch (query)
        {
            case ExplicitTableQuery node:
                VisitExplicitTableQuery(node);
                return true;
            case CopyStatement node:
                VisitCopyStatement(node);
                return true;
            default:
                return false;
        }
    }

    internal bool TryVisitTableSourcePgDml(TableSource table)
    {
        switch (table)
        {
            case RowsFromTable node:
                VisitRowsFromTable(node);
                return true;
            case DescendantTable node:
                VisitDescendantTable(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitDescendantTable(DescendantTable node)
    {
        return;
    }

    public virtual void VisitExplicitTableQuery(ExplicitTableQuery node)
    {
        return;
    }

    public virtual void VisitCopyStatement(CopyStatement node)
    {
        if (node.Query is not null)
        {
            Visit(node.Query);
        }

        foreach (var option in node.Options)
        {
            VisitCopyOption(option);
        }

        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }
    }

    public virtual void VisitCopyOption(CopyOption node)
    {
        return;
    }

    public virtual void VisitRowsFromTable(RowsFromTable node)
    {
        foreach (var function in node.Functions)
        {
            VisitRowsFromFunction(function);
        }

        if (node.Ordinality is not null)
        {
            VisitOrdinalityClause(node.Ordinality);
        }
    }

    public virtual void VisitOrdinalityClause(OrdinalityClause node)
    {
        return;
    }

    public virtual void VisitMaterializationClause(MaterializationClause node)
    {
        return;
    }

    public virtual void VisitLockStrengthModifier(LockStrengthModifier node)
    {
        return;
    }

    public virtual void VisitLockWaitPolicy(LockWaitPolicy node)
    {
        return;
    }

    public virtual void VisitRowsFromFunction(RowsFromFunction node)
    {
        Visit(node.Function);
        VisitFieldDefinitions(node.ColumnDefinitions);
    }

    public virtual void VisitDistinctOnClause(DistinctOnClause node) => VisitAll(node.Expressions);

    public virtual void VisitSelectIntoTable(SelectIntoTable node)
    {
        return;
    }

    public virtual void VisitReturningClause(ReturningClause node)
    {
        foreach (var alias in node.OutputAliases)
        {
            VisitReturningOutputAlias(alias);
        }

        foreach (var item in node.Items)
        {
            VisitSelectItem(item);
        }
    }

    public virtual void VisitReturningOutputAlias(ReturningOutputAlias node)
    {
        return;
    }

    public virtual void VisitOnConflictClause(OnConflictClause node)
    {
        if (node.Target is not null)
        {
            VisitConflictTarget(node.Target);
        }

        VisitSetClauses(node.Assignments);
        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }
    }

    public virtual void VisitConflictTarget(ConflictTarget node)
    {
        foreach (var element in node.Elements)
        {
            VisitConflictTargetElement(element);
        }

        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }
    }

    public virtual void VisitConflictTargetElement(ConflictTargetElement node) => Visit(node.Expression);

    internal void VisitOptionalReturning(ReturningClause? returning)
    {
        if (returning is not null)
        {
            VisitReturningClause(returning);
        }
    }

    internal void VisitOptionalFrom(TableSource? from, IReadOnlyList<JoinClause> joins, IReadOnlyList<CommaFrom> extra)
    {
        if (from is null)
        {
            return;
        }

        Visit(from);
        VisitJoins(joins);
        foreach (var item in extra)
        {
            VisitCommaFrom(item);
        }
    }

    internal void VisitFieldDefinitions(IReadOnlyList<FieldDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            VisitDataType(definition.Type);
        }
    }
}
