namespace QlParse;

public partial class SqlVisitor
{
    // Workstream C: SQL Server-only query and DML statement nodes.
    internal bool TryVisitTsqlDmlQuery(Query query)
    {
        switch (query)
        {
            case TsqlInsertStatement node:
                VisitTsqlInsertStatement(node);
                return true;
            case TsqlUpdateStatement node:
                VisitTsqlUpdateStatement(node);
                return true;
            case TsqlDeleteStatement node:
                VisitTsqlDeleteStatement(node);
                return true;
            case TsqlMergeStatement node:
                VisitTsqlMergeStatement(node);
                return true;
            case TsqlBulkInsertStatement node:
                VisitTsqlBulkInsertStatement(node);
                return true;
            case TsqlWithXmlNamespacesQuery node:
                VisitTsqlWithXmlNamespacesQuery(node);
                return true;
            default:
                return false;
        }
    }

    // Workstream C: SQL Server FROM items.
    internal bool TryVisitTsqlDmlTableSource(TableSource table)
    {
        switch (table)
        {
            case TsqlHintedTable node:
                VisitTsqlHintedTable(node);
                return true;
            case TsqlPivotTable node:
                VisitTsqlPivotTable(node);
                return true;
            case TsqlUnpivotTable node:
                VisitTsqlUnpivotTable(node);
                return true;
            case TsqlSampledTable node:
                VisitTsqlSampledTable(node);
                return true;
            case TsqlTemporalTable node:
                VisitTsqlTemporalTable(node);
                return true;
            case TsqlRowsetFunctionTable node:
                VisitTsqlRowsetFunctionTable(node);
                return true;
            default:
                return false;
        }
    }

    // Workstream C: `@v = expr` in a SELECT list.
    internal bool TryVisitTsqlDmlExpression(Expression expression)
    {
        if (expression is not TsqlVariableAssignmentExpression node)
        {
            return false;
        }

        VisitTsqlVariableAssignmentExpression(node);
        return true;
    }

    // Workstream C: SQL Server MERGE actions.
    internal bool TryVisitTsqlDmlMergeAction(MergeAction action)
    {
        switch (action)
        {
            case TsqlMergeUpdateAction node:
                VisitTsqlMergeUpdateAction(node);
                return true;
            case TsqlMergeInsertDefaultAction node:
                VisitTsqlMergeInsertDefaultAction(node);
                return true;
            default:
                return false;
        }
    }

    // ---- Statements ----

    public virtual void VisitTsqlInsertStatement(TsqlInsertStatement node)
    {
        VisitTsqlTop(node.Top);
        Visit(node.Target);
        VisitTsqlOutputClauses(node.Output);
        if (node.Source is not null)
        {
            Visit(node.Source);
        }

        VisitTsqlOption(node.Option);
    }

    public virtual void VisitTsqlUpdateStatement(TsqlUpdateStatement node)
    {
        VisitTsqlTop(node.Top);
        Visit(node.Target);
        VisitTsqlSetClauses(node.Assignments);
        VisitTsqlOutputClauses(node.Output);
        VisitTsqlDmlFrom(node.From, node.Joins, node.ExtraFrom);
        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }

        VisitTsqlOption(node.Option);
    }

    public virtual void VisitTsqlDeleteStatement(TsqlDeleteStatement node)
    {
        VisitTsqlTop(node.Top);
        Visit(node.Target);
        VisitTsqlOutputClauses(node.Output);
        VisitTsqlDmlFrom(node.From, node.Joins, node.ExtraFrom);
        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }

        VisitTsqlOption(node.Option);
    }

    public virtual void VisitTsqlMergeStatement(TsqlMergeStatement node)
    {
        VisitTsqlTop(node.Top);
        Visit(node.Target);
        Visit(node.Source);
        VisitJoins(node.SourceJoins);
        Visit(node.Condition);
        foreach (var when in node.Whens)
        {
            VisitMergeWhenClause(when);
        }

        VisitTsqlOutputClauses(node.Output);
        VisitTsqlOption(node.Option);
    }

    public virtual void VisitTsqlBulkInsertStatement(TsqlBulkInsertStatement node)
    {
        foreach (var option in node.Options)
        {
            VisitTsqlBulkInsertOption(option);
        }
    }

    public virtual void VisitTsqlBulkInsertOption(TsqlBulkInsertOption node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }

        foreach (var item in node.Order)
        {
            VisitOrderByItem(item);
        }
    }

    public virtual void VisitTsqlWithXmlNamespacesQuery(TsqlWithXmlNamespacesQuery node)
    {
        foreach (var cte in node.Ctes)
        {
            VisitCommonTableExpression(cte);
        }

        Visit(node.Query);
    }

    public virtual void VisitTsqlTruncateExtras(TsqlTruncateExtras node)
    {
        foreach (var partition in node.Partitions)
        {
            Visit(partition.Start);
            if (partition.End is not null)
            {
                Visit(partition.End);
            }
        }
    }

    // ---- SELECT extras ----

    public virtual void VisitTsqlSelectExtras(TsqlSelectExtras node)
    {
        VisitTsqlTop(node.Top);
        foreach (var compute in node.Compute)
        {
            VisitTsqlComputeClause(compute);
        }

        if (node.For is not null)
        {
            VisitTsqlForClause(node.For);
        }

        VisitTsqlOption(node.Option);
    }

    public virtual void VisitTsqlTopClause(TsqlTopClause node) => Visit(node.Count);

    public virtual void VisitTsqlComputeClause(TsqlComputeClause node)
    {
        VisitAll(node.Functions);
        VisitAll(node.By);
    }

    public virtual void VisitTsqlForClause(TsqlForClause node)
    {
        foreach (var option in node.Options)
        {
            if (option.Argument is not null)
            {
                Visit(option.Argument);
            }
        }
    }

    public virtual void VisitTsqlOptionClause(TsqlOptionClause node) => VisitTsqlHints(node.Hints);

    public virtual void VisitTsqlHint(TsqlHint node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }

        foreach (var argument in node.Arguments)
        {
            Visit(argument.Value);
            if (argument.Assigned is not null)
            {
                Visit(argument.Assigned);
            }
        }
    }

    public virtual void VisitTsqlVariableAssignmentExpression(TsqlVariableAssignmentExpression node) => Visit(node.Value);

    // ---- DML pieces ----

    public virtual void VisitTsqlOutputClause(TsqlOutputClause node)
    {
        foreach (var column in node.Columns)
        {
            VisitSelectItem(column);
        }
    }

    public virtual void VisitTsqlSetClause(TsqlSetClause node)
    {
        if (node.Column is not null)
        {
            Visit(node.Column);
        }

        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitTsqlMergeUpdateAction(TsqlMergeUpdateAction node) => VisitTsqlSetClauses(node.Assignments);

    public virtual void VisitTsqlMergeInsertDefaultAction(TsqlMergeInsertDefaultAction node)
    {
        return;
    }

    // ---- FROM items ----

    public virtual void VisitTsqlHintedTable(TsqlHintedTable node)
    {
        Visit(node.Table);
        VisitTsqlHints(node.Hints);
    }

    public virtual void VisitTsqlPivotTable(TsqlPivotTable node)
    {
        Visit(node.Table);
        Visit(node.Aggregate);
        Visit(node.Column);
    }

    public virtual void VisitTsqlUnpivotTable(TsqlUnpivotTable node) => Visit(node.Table);

    public virtual void VisitTsqlSampledTable(TsqlSampledTable node)
    {
        Visit(node.Table);
        Visit(node.Size);
        if (node.Seed is not null)
        {
            Visit(node.Seed);
        }
    }

    public virtual void VisitTsqlTemporalTable(TsqlTemporalTable node)
    {
        Visit(node.Table);
        Visit(node.Start);
        Visit(node.End);
    }

    public virtual void VisitTsqlRowsetFunctionTable(TsqlRowsetFunctionTable node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument.Value);
        }

        if (node.Schema is null)
        {
            return;
        }

        foreach (var column in node.Schema.Columns)
        {
            VisitDataType(column.Type);
        }
    }

    private void VisitTsqlTop(TsqlTopClause? top)
    {
        if (top is not null)
        {
            VisitTsqlTopClause(top);
        }
    }

    private void VisitTsqlOption(TsqlOptionClause? option)
    {
        if (option is not null)
        {
            VisitTsqlOptionClause(option);
        }
    }

    private void VisitTsqlHints(IReadOnlyList<TsqlHint> hints)
    {
        foreach (var hint in hints)
        {
            VisitTsqlHint(hint);
        }
    }

    private void VisitTsqlOutputClauses(IReadOnlyList<TsqlOutputClause> clauses)
    {
        foreach (var clause in clauses)
        {
            VisitTsqlOutputClause(clause);
        }
    }

    private void VisitTsqlSetClauses(IReadOnlyList<TsqlSetClause> clauses)
    {
        foreach (var clause in clauses)
        {
            VisitTsqlSetClause(clause);
        }
    }

    private void VisitTsqlDmlFrom(TableSource? from, IReadOnlyList<JoinClause> joins, IReadOnlyList<CommaFrom> extraFrom)
    {
        if (from is not null)
        {
            Visit(from);
        }

        VisitJoins(joins);
        foreach (var extra in extraFrom)
        {
            VisitCommaFrom(extra);
        }
    }
}
