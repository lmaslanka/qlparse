namespace QlParse;

public partial class SqlVisitor
{
    private bool TryVisitPgUtilityQuery1(Query query)
    {
        switch (query)
        {
            case BeginTransactionStatement node:
                VisitBeginTransactionStatement(node);
                return true;
            case PrepareTransactionStatement node:
                VisitPrepareTransactionStatement(node);
                return true;
            case CommitPreparedStatement node:
                VisitCommitPreparedStatement(node);
                return true;
            case RollbackPreparedStatement node:
                VisitRollbackPreparedStatement(node);
                return true;
            case SetConfigurationStatement node:
                VisitSetConfigurationStatement(node);
                return true;
            case ShowStatement node:
                VisitShowStatement(node);
                return true;
            case ResetStatement node:
                VisitResetStatement(node);
                return true;
            case AlterSystemStatement node:
                VisitAlterSystemStatement(node);
                return true;
            case PrepareAsStatement node:
                VisitPrepareAsStatement(node);
                return true;
            case ExecutePreparedStatement node:
                VisitExecutePreparedStatement(node);
                return true;
            case DeallocatePreparedStatement node:
                VisitDeallocatePreparedStatement(node);
                return true;
            case LockTableStatement node:
                VisitLockTableStatement(node);
                return true;
            case ExplainStatement node:
                VisitExplainStatement(node);
                return true;
            case VacuumStatement node:
                VisitVacuumStatement(node);
                return true;
            case AnalyzeStatement node:
                VisitAnalyzeStatement(node);
                return true;
            case ClusterStatement node:
                VisitClusterStatement(node);
                return true;
            case CheckpointStatement node:
                VisitCheckpointStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitPgUtilityQuery2(Query query)
    {
        switch (query)
        {
            case ListenStatement node:
                VisitListenStatement(node);
                return true;
            case NotifyStatement node:
                VisitNotifyStatement(node);
                return true;
            case UnlistenStatement node:
                VisitUnlistenStatement(node);
                return true;
            case LoadStatement node:
                VisitLoadStatement(node);
                return true;
            case DiscardStatement node:
                VisitDiscardStatement(node);
                return true;
            case AlterRoleStatement node:
                VisitAlterRoleStatement(node);
                return true;
            case ReassignOwnedStatement node:
                VisitReassignOwnedStatement(node);
                return true;
            case DropOwnedStatement node:
                VisitDropOwnedStatement(node);
                return true;
            case AlterDefaultPrivilegesStatement node:
                VisitAlterDefaultPrivilegesStatement(node);
                return true;
            case CreatePolicyStatement node:
                VisitCreatePolicyStatement(node);
                return true;
            case AlterPolicyStatement node:
                VisitAlterPolicyStatement(node);
                return true;
            case DropPolicyStatement node:
                VisitDropPolicyStatement(node);
                return true;
            case SecurityLabelStatement node:
                VisitSecurityLabelStatement(node);
                return true;
            case CreateDatabaseStatement node:
                VisitCreateDatabaseStatement(node);
                return true;
            case AlterDatabaseStatement node:
                VisitAlterDatabaseStatement(node);
                return true;
            case DropDatabaseStatement node:
                VisitDropDatabaseStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitPgUtilityQuery(Query query) => TryVisitPgUtilityQuery1(query) || TryVisitPgUtilityQuery2(query);

    public virtual void VisitBeginTransactionStatement(BeginTransactionStatement node) => VisitTransactionModes(node.Modes);

    public virtual void VisitPrepareTransactionStatement(PrepareTransactionStatement node)
    {
        return;
    }

    public virtual void VisitCommitPreparedStatement(CommitPreparedStatement node)
    {
        return;
    }

    public virtual void VisitRollbackPreparedStatement(RollbackPreparedStatement node)
    {
        return;
    }

    public virtual void VisitSetConfigurationStatement(SetConfigurationStatement node)
    {
        return;
    }

    public virtual void VisitShowStatement(ShowStatement node)
    {
        return;
    }

    public virtual void VisitResetStatement(ResetStatement node)
    {
        return;
    }

    public virtual void VisitAlterSystemStatement(AlterSystemStatement node) => Visit(node.Action);

    public virtual void VisitPrepareAsStatement(PrepareAsStatement node)
    {
        foreach (var type in node.ParameterTypes)
        {
            VisitDataType(type);
        }

        Visit(node.Statement);
    }

    public virtual void VisitExecutePreparedStatement(ExecutePreparedStatement node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
    }

    public virtual void VisitDeallocatePreparedStatement(DeallocatePreparedStatement node)
    {
        return;
    }

    public virtual void VisitLockTableStatement(LockTableStatement node)
    {
        return;
    }

    public virtual void VisitExplainStatement(ExplainStatement node) => Visit(node.Statement);

    public virtual void VisitVacuumStatement(VacuumStatement node)
    {
        return;
    }

    public virtual void VisitAnalyzeStatement(AnalyzeStatement node)
    {
        return;
    }

    public virtual void VisitClusterStatement(ClusterStatement node)
    {
        return;
    }

    public virtual void VisitCheckpointStatement(CheckpointStatement node)
    {
        return;
    }

    public virtual void VisitListenStatement(ListenStatement node)
    {
        return;
    }

    public virtual void VisitNotifyStatement(NotifyStatement node)
    {
        return;
    }

    public virtual void VisitUnlistenStatement(UnlistenStatement node)
    {
        return;
    }

    public virtual void VisitLoadStatement(LoadStatement node)
    {
        return;
    }

    public virtual void VisitDiscardStatement(DiscardStatement node)
    {
        return;
    }

    public virtual void VisitAlterRoleStatement(AlterRoleStatement node)
    {
        if (node.Setting is not null)
        {
            Visit(node.Setting);
        }
    }

    public virtual void VisitReassignOwnedStatement(ReassignOwnedStatement node)
    {
        return;
    }

    public virtual void VisitDropOwnedStatement(DropOwnedStatement node)
    {
        return;
    }

    public virtual void VisitAlterDefaultPrivilegesStatement(AlterDefaultPrivilegesStatement node) => Visit(node.Action);

    public virtual void VisitCreatePolicyStatement(CreatePolicyStatement node)
    {
        VisitPolicyConditions(node.Using, node.WithCheck);
    }

    public virtual void VisitAlterPolicyStatement(AlterPolicyStatement node)
    {
        VisitPolicyConditions(node.Using, node.WithCheck);
    }

    public virtual void VisitDropPolicyStatement(DropPolicyStatement node)
    {
        return;
    }

    public virtual void VisitSecurityLabelStatement(SecurityLabelStatement node) => VisitObjectSignature(node.Object);

    public virtual void VisitCreateDatabaseStatement(CreateDatabaseStatement node)
    {
        return;
    }

    public virtual void VisitAlterDatabaseStatement(AlterDatabaseStatement node)
    {
        if (node.Setting is not null)
        {
            Visit(node.Setting);
        }
    }

    public virtual void VisitDropDatabaseStatement(DropDatabaseStatement node)
    {
        return;
    }

    public virtual void VisitRowLevelSecurityAction(RowLevelSecurityAction node)
    {
        return;
    }

    public virtual void VisitPolicyConditions(PolicyCondition? usingCondition, PolicyCondition? withCheck)
    {
        if (usingCondition is not null)
        {
            VisitPolicyCondition(usingCondition);
        }

        if (withCheck is not null)
        {
            VisitPolicyCondition(withCheck);
        }
    }

    public virtual void VisitPolicyCondition(PolicyCondition node) => Visit(node.Expression);

    public virtual void VisitObjectSignature(ObjectSignature node)
    {
        foreach (var type in node.ArgumentTypes)
        {
            VisitDataType(type);
        }
    }
}
