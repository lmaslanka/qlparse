namespace QlParse;

public partial class SqlVisitor
{
    // Workstream D: SQL Server-only DDL statement nodes.
    internal bool TryVisitTsqlDdlQuery(Query query) => TryVisitTsqlDdlIndexQuery(query) || TryVisitTsqlDdlObjectQuery(query);

    private bool TryVisitTsqlDdlIndexQuery(Query query)
    {
        switch (query)
        {
            case TsqlCreateSpecialIndexStatement node:
                VisitTsqlCreateSpecialIndexStatement(node);
                return true;
            case TsqlAlterIndexStatement node:
                VisitTsqlAlterIndexStatement(node);
                return true;
            case TsqlDropIndexStatement node:
                VisitTsqlDropIndexStatement(node);
                return true;
            case TsqlCreateStatisticsStatement node:
                VisitTsqlCreateStatisticsStatement(node);
                return true;
            case TsqlUpdateStatisticsStatement node:
                VisitTsqlUpdateStatisticsStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitTsqlDdlObjectQuery(Query query)
    {
        switch (query)
        {
            case TsqlDropObjectsStatement node:
                VisitTsqlDropObjectsStatement(node);
                return true;
            case TsqlCreateSynonymStatement node:
                VisitTsqlCreateSynonymStatement(node);
                return true;
            case TsqlCreatePartitionFunctionStatement node:
                VisitTsqlCreatePartitionFunctionStatement(node);
                return true;
            case TsqlCreatePartitionSchemeStatement node:
                VisitTsqlCreatePartitionSchemeStatement(node);
                return true;
            case TsqlAlterPartitionFunctionStatement node:
                VisitTsqlAlterPartitionFunctionStatement(node);
                return true;
            case TsqlAlterPartitionSchemeStatement node:
                VisitTsqlAlterPartitionSchemeStatement(node);
                return true;
            case TsqlCreateTypeStatement node:
                VisitTsqlCreateTypeStatement(node);
                return true;
            default:
                return false;
        }
    }

    // Workstream D: SQL Server graph expressions (MATCH, WITHIN GROUP (GRAPH PATH)).
    internal bool TryVisitTsqlDdlExpression(Expression expression)
    {
        switch (expression)
        {
            case TsqlMatchExpression node:
                VisitTsqlMatchExpression(node);
                return true;
            case TsqlGraphPathAggregateExpression node:
                VisitTsqlGraphPathAggregateExpression(node);
                return true;
            default:
                return false;
        }
    }

    internal bool TryVisitTsqlDdlTableSource(TableSource table)
    {
        if (table is TsqlGraphPathTable node)
        {
            VisitTsqlGraphPathTable(node);
            return true;
        }

        return false;
    }

    internal bool TryVisitTsqlDdlTableElement(TableElement element)
    {
        if (element is TsqlTableIndex node)
        {
            VisitTsqlTableIndex(node);
            return true;
        }

        return false;
    }

    internal bool TryVisitTsqlDdlAlterTableAction(AlterTableAction action)
    {
        switch (action)
        {
            case TsqlAddElementsAction node:
                VisitTsqlAddElementsAction(node);
                return true;
            case TsqlDropElementsAction node:
                VisitTsqlDropElementsAction(node);
                return true;
            case TsqlAlterColumnAction node:
                VisitTsqlAlterColumnAction(node);
                return true;
            case TsqlCheckConstraintAction node:
                VisitTsqlCheckConstraintAction(node);
                return true;
            case TsqlToggleTriggersAction node:
                VisitTsqlToggleTriggersAction(node);
                return true;
            case TsqlSwitchPartitionAction node:
                VisitTsqlSwitchPartitionAction(node);
                return true;
            case TsqlSetTableOptionsAction node:
                VisitTsqlSetTableOptionsAction(node);
                return true;
            case TsqlRebuildAction node:
                VisitTsqlRebuildAction(node);
                return true;
            default:
                return false;
        }
    }

    // ---- Extras of ISO nodes ----

    public virtual void VisitTsqlCreateTableExtras(TsqlCreateTableExtras node)
    {
        return;
    }

    public virtual void VisitTsqlColumnExtras(TsqlColumnExtras node)
    {
        if (node.Computed is not null)
        {
            Visit(node.Computed.Expression);
        }

        if (node.Identity is not null)
        {
            VisitTsqlIdentitySpec(node.Identity);
        }

        if (node.Index is not null)
        {
            VisitTsqlTableIndex(node.Index);
        }
    }

    public virtual void VisitTsqlIdentitySpec(TsqlIdentitySpec node)
    {
        if (node.Seed is not null)
        {
            Visit(node.Seed);
        }

        if (node.Increment is not null)
        {
            Visit(node.Increment);
        }
    }

    public virtual void VisitTsqlTableConstraintExtras(TsqlTableConstraintExtras node)
    {
        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }
    }

    public virtual void VisitTsqlCreateIndexExtras(TsqlCreateIndexExtras node)
    {
        return;
    }

    // ---- Tables ----

    public virtual void VisitTsqlTableIndex(TsqlTableIndex node)
    {
        if (node.Columns is not null)
        {
            VisitTsqlIndexColumns(node.Columns);
        }

        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    private void VisitTsqlIndexColumns(IReadOnlyList<IndexColumn> columns)
    {
        foreach (var column in columns)
        {
            Visit(column.Expression);
        }
    }

    public virtual void VisitTsqlAddElementsAction(TsqlAddElementsAction node)
    {
        foreach (var element in node.Elements)
        {
            Visit(element);
        }
    }

    public virtual void VisitTsqlDropElementsAction(TsqlDropElementsAction node)
    {
        return;
    }

    public virtual void VisitTsqlAlterColumnAction(TsqlAlterColumnAction node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }
    }

    public virtual void VisitTsqlCheckConstraintAction(TsqlCheckConstraintAction node)
    {
        return;
    }

    public virtual void VisitTsqlToggleTriggersAction(TsqlToggleTriggersAction node)
    {
        return;
    }

    public virtual void VisitTsqlSwitchPartitionAction(TsqlSwitchPartitionAction node)
    {
        if (node.SourcePartition is not null)
        {
            Visit(node.SourcePartition);
        }

        if (node.TargetPartition is not null)
        {
            Visit(node.TargetPartition);
        }
    }

    public virtual void VisitTsqlSetTableOptionsAction(TsqlSetTableOptionsAction node)
    {
        return;
    }

    public virtual void VisitTsqlRebuildAction(TsqlRebuildAction node)
    {
        return;
    }

    // ---- Indexes and statistics ----

    public virtual void VisitTsqlCreateSpecialIndexStatement(TsqlCreateSpecialIndexStatement node)
    {
        if (node.Columns is not null)
        {
            VisitTsqlIndexColumns(node.Columns);
        }

        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitTsqlAlterIndexStatement(TsqlAlterIndexStatement node)
    {
        return;
    }

    public virtual void VisitTsqlDropIndexStatement(TsqlDropIndexStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateStatisticsStatement(TsqlCreateStatisticsStatement node)
    {
        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitTsqlUpdateStatisticsStatement(TsqlUpdateStatisticsStatement node)
    {
        return;
    }

    // ---- Other objects ----

    public virtual void VisitTsqlDropObjectsStatement(TsqlDropObjectsStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateSynonymStatement(TsqlCreateSynonymStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreatePartitionFunctionStatement(TsqlCreatePartitionFunctionStatement node)
    {
        VisitDataType(node.ParameterType);
        foreach (var value in node.Values)
        {
            Visit(value);
        }
    }

    public virtual void VisitTsqlCreatePartitionSchemeStatement(TsqlCreatePartitionSchemeStatement node)
    {
        return;
    }

    public virtual void VisitTsqlAlterPartitionFunctionStatement(TsqlAlterPartitionFunctionStatement node) =>
        Visit(node.Value);

    public virtual void VisitTsqlAlterPartitionSchemeStatement(TsqlAlterPartitionSchemeStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateTypeStatement(TsqlCreateTypeStatement node)
    {
        if (node.BaseType is not null)
        {
            VisitDataType(node.BaseType);
        }

        if (node.Elements is null)
        {
            return;
        }

        foreach (var element in node.Elements)
        {
            Visit(element);
        }
    }

    // ---- Graph ----

    public virtual void VisitTsqlMatchExpression(TsqlMatchExpression node)
    {
        foreach (var pattern in node.Patterns)
        {
            VisitTsqlGraphPattern(pattern);
        }
    }

    public virtual void VisitTsqlGraphPattern(TsqlGraphPattern node)
    {
        return;
    }

    public virtual void VisitTsqlGraphPathTable(TsqlGraphPathTable node)
    {
        return;
    }

    public virtual void VisitTsqlGraphPathAggregateExpression(TsqlGraphPathAggregateExpression node) =>
        Visit(node.Call);
}
