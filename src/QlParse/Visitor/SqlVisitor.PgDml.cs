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

    public virtual void VisitPgSelectExtras(PgSelectExtras node)
    {
        if (node.DistinctOn is not null)
        {
            VisitDistinctOnClause(node.DistinctOn);
        }

        if (node.IntoTable is not null)
        {
            VisitSelectIntoTable(node.IntoTable);
        }
    }

    public virtual void VisitPgLimitExtras(PgLimitExtras node)
    {
        return;
    }

    public virtual void VisitPgLockExtras(PgLockExtras node)
    {
        if (node.NextLock is not null)
        {
            VisitLockClause(node.NextLock);
        }
    }

    public virtual void VisitPgTableFunctionExtras(PgTableFunctionExtras node) => VisitFieldDefinitions(node.ColumnDefinitions);

    public virtual void VisitPgCteExtras(PgCteExtras node)
    {
        return;
    }

    public virtual void VisitPgInsertExtras(PgInsertExtras node)
    {
        if (node.OnConflict is not null)
        {
            VisitOnConflictClause(node.OnConflict);
        }

        VisitOptionalReturning(node.Returning);
    }

    public virtual void VisitPgUpdateExtras(PgUpdateExtras node)
    {
        VisitOptionalFrom(node.From, node.Joins, node.ExtraFrom);
        VisitOptionalReturning(node.Returning);
    }

    public virtual void VisitPgDeleteExtras(PgDeleteExtras node)
    {
        VisitOptionalFrom(node.Using, node.UsingJoins, node.ExtraUsing);
        VisitOptionalReturning(node.Returning);
    }

    public virtual void VisitPgMergeExtras(PgMergeExtras node) => VisitOptionalReturning(node.Returning);

    public virtual void VisitPgTruncateExtras(PgTruncateExtras node)
    {
        return;
    }

    private void VisitOptionalReturning(ReturningClause? returning)
    {
        if (returning is not null)
        {
            VisitReturningClause(returning);
        }
    }

    private void VisitOptionalFrom(TableSource? from, IReadOnlyList<JoinClause> joins, IReadOnlyList<CommaFrom> extra)
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

    private void VisitFieldDefinitions(IReadOnlyList<FieldDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            VisitDataType(definition.Type);
        }
    }
}
