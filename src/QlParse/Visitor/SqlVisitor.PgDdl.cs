namespace QlParse;

public partial class SqlVisitor
{
    // Postgres DDL statements; dispatched after every ISO bucket so ISO nodes keep their dispatch cost.
    private bool TryVisitPgDdlQuery(Query query)
    {
        switch (query)
        {
            case CreateMaterializedViewStatement node:
                VisitCreateMaterializedViewStatement(node);
                return true;
            case RefreshMaterializedViewStatement node:
                VisitRefreshMaterializedViewStatement(node);
                return true;
            case CreateTablespaceStatement node:
                VisitCreateTablespaceStatement(node);
                return true;
            case AlterTablespaceStatement node:
                VisitAlterTablespaceStatement(node);
                return true;
            case DropTablespaceStatement node:
                VisitDropTablespaceStatement(node);
                return true;
            case CreateRuleStatement node:
                VisitCreateRuleStatement(node);
                return true;
            case DropRuleStatement node:
                VisitDropRuleStatement(node);
                return true;
            case ReindexStatement node:
                VisitReindexStatement(node);
                return true;
            case AlterSequenceStatement node:
                VisitAlterSequenceStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitPgDdlAlterTableAction(AlterTableAction action)
    {
        switch (action)
        {
            case RenameRelationAction node:
                VisitRenameRelationAction(node);
                return true;
            case SetRelationSchemaAction node:
                VisitSetRelationSchemaAction(node);
                return true;
            case RelationOwnerAction node:
                VisitRelationOwnerAction(node);
                return true;
            case SetTablespaceAction node:
                VisitSetTablespaceAction(node);
                return true;
            case SetPersistenceAction node:
                VisitSetPersistenceAction(node);
                return true;
            case RelationOptionsAction node:
                VisitRelationOptionsAction(node);
                return true;
            case InheritAction node:
                VisitInheritAction(node);
                return true;
            default:
                return TryVisitPgDdlAlterTableAction2(action);
        }
    }

    private bool TryVisitPgDdlAlterTableAction2(AlterTableAction action)
    {
        switch (action)
        {
            case ClusterAction node:
                VisitClusterAction(node);
                return true;
            case ToggleTriggerAction node:
                VisitToggleTriggerAction(node);
                return true;
            case ReplicaIdentityAction node:
                VisitReplicaIdentityAction(node);
                return true;
            case AttachPartitionAction node:
                VisitAttachPartitionAction(node);
                return true;
            case DetachPartitionAction node:
                VisitDetachPartitionAction(node);
                return true;
            case ValidateTableConstraintAction node:
                VisitValidateTableConstraintAction(node);
                return true;
            default:
                return TryVisitPgGapAlterTableAction(action);
        }
    }

    private bool TryVisitPgDdlAlterViewAction(AlterViewAction action)
    {
        if (action is not AlterViewRelationAction node)
        {
            return false;
        }

        VisitAlterViewRelationAction(node);
        return true;
    }

    // Postgres-only parts of ISO DDL nodes; each extras object is null unless a Postgres clause was parsed.
    public virtual void VisitPgCreateTableExtras(PgCreateTableExtras node)
    {
        if (node.PartitionOf is not null)
        {
            VisitPartitionOfClause(node.PartitionOf);
        }

        if (node.PartitionBy is not null)
        {
            VisitPartitionByClause(node.PartitionBy);
        }
    }

    public virtual void VisitPgTableConstraintExtras(PgTableConstraintExtras node)
    {
        if (node.Exclusion is not null)
        {
            VisitExclusionClause(node.Exclusion);
        }
    }

    public virtual void VisitPgAlterColumnExtras(PgAlterColumnExtras node)
    {
        if (node.Using is not null)
        {
            Visit(node.Using);
        }

        if (node.AttributeValue is not null)
        {
            Visit(node.AttributeValue);
        }

        if (node.Identity is not null)
        {
            VisitIdentityColumn(node.Identity);
        }
    }

    public virtual void VisitPgAlterTableExtras(PgAlterTableExtras node)
    {
        foreach (var action in node.AdditionalActions)
        {
            Visit(action);
        }
    }

    public virtual void VisitPgAlterIndexExtras(PgAlterIndexExtras node)
    {
        if (node.RelationAction is not null)
        {
            Visit(node.RelationAction);
        }
    }

    public virtual void VisitPartitionOfClause(PartitionOfClause node) => VisitPartitionBound(node.Bound);

    public virtual void VisitPartitionBound(PartitionBound node)
    {
        VisitAll(node.Values);
        VisitAll(node.ToValues);
    }

    public virtual void VisitPartitionByClause(PartitionByClause node)
    {
        foreach (var column in node.Columns)
        {
            Visit(column.Expression);
        }
    }

    public virtual void VisitExclusionClause(ExclusionClause node)
    {
        foreach (var element in node.Elements)
        {
            Visit(element.Column.Expression);
        }

        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitCreateMaterializedViewStatement(CreateMaterializedViewStatement node) => Visit(node.Query);

    public virtual void VisitRefreshMaterializedViewStatement(RefreshMaterializedViewStatement node)
    {
        return;
    }

    public virtual void VisitCreateTablespaceStatement(CreateTablespaceStatement node)
    {
        return;
    }

    public virtual void VisitAlterTablespaceStatement(AlterTablespaceStatement node) => Visit(node.Action);

    public virtual void VisitDropTablespaceStatement(DropTablespaceStatement node)
    {
        return;
    }

    public virtual void VisitCreateRuleStatement(CreateRuleStatement node)
    {
        if (node.Where is not null)
        {
            Visit(node.Where);
        }

        foreach (var command in node.Commands)
        {
            Visit(command);
        }
    }

    public virtual void VisitDropRuleStatement(DropRuleStatement node)
    {
        return;
    }

    public virtual void VisitReindexStatement(ReindexStatement node)
    {
        return;
    }

    public virtual void VisitAlterSequenceStatement(AlterSequenceStatement node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }

        foreach (var option in node.Options)
        {
            VisitSequenceOption(option);
        }

        if (node.Action is not null)
        {
            Visit(node.Action);
        }
    }

    public virtual void VisitAlterViewRelationAction(AlterViewRelationAction node) => Visit(node.Action);

    public virtual void VisitRenameRelationAction(RenameRelationAction node)
    {
        return;
    }

    public virtual void VisitSetRelationSchemaAction(SetRelationSchemaAction node)
    {
        return;
    }

    public virtual void VisitRelationOwnerAction(RelationOwnerAction node)
    {
        return;
    }

    public virtual void VisitSetTablespaceAction(SetTablespaceAction node)
    {
        return;
    }

    public virtual void VisitSetPersistenceAction(SetPersistenceAction node)
    {
        return;
    }

    public virtual void VisitRelationOptionsAction(RelationOptionsAction node)
    {
        return;
    }

    public virtual void VisitInheritAction(InheritAction node)
    {
        return;
    }

    public virtual void VisitClusterAction(ClusterAction node)
    {
        return;
    }

    public virtual void VisitToggleTriggerAction(ToggleTriggerAction node)
    {
        return;
    }

    public virtual void VisitReplicaIdentityAction(ReplicaIdentityAction node)
    {
        return;
    }

    public virtual void VisitAttachPartitionAction(AttachPartitionAction node)
    {
        if (node.Bound is not null)
        {
            VisitPartitionBound(node.Bound);
        }
    }

    public virtual void VisitDetachPartitionAction(DetachPartitionAction node)
    {
        return;
    }

    public virtual void VisitValidateTableConstraintAction(ValidateTableConstraintAction node)
    {
        return;
    }
}
