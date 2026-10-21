namespace QlParse;

public partial class SqlVisitor
{
    // Workstream G: SQL Server-only administration, Service Broker and full-text statement nodes.
    internal bool TryVisitTsqlAdminQuery(Query query) =>
        TryVisitTsqlAdminQuery1(query) || TryVisitTsqlAdminQuery2(query);

    private bool TryVisitTsqlAdminQuery1(Query query)
    {
        switch (query)
        {
            case TsqlDbccStatement node:
                VisitTsqlDbccStatement(node);
                return true;
            case TsqlBackupStatement node:
                VisitTsqlBackupStatement(node);
                return true;
            case TsqlRestoreStatement node:
                VisitTsqlRestoreStatement(node);
                return true;
            case TsqlCheckpointStatement node:
                VisitTsqlCheckpointStatement(node);
                return true;
            case TsqlKillStatement node:
                VisitTsqlKillStatement(node);
                return true;
            case TsqlReconfigureStatement node:
                VisitTsqlReconfigureStatement(node);
                return true;
            case TsqlShutdownStatement node:
                VisitTsqlShutdownStatement(node);
                return true;
            case TsqlSetUserStatement node:
                VisitTsqlSetUserStatement(node);
                return true;
            case TsqlReadTextStatement node:
                VisitTsqlReadTextStatement(node);
                return true;
            case TsqlWriteTextStatement node:
                VisitTsqlWriteTextStatement(node);
                return true;
            case TsqlUpdateTextStatement node:
                VisitTsqlUpdateTextStatement(node);
                return true;
            case TsqlAlterServerConfigurationStatement node:
                VisitTsqlAlterServerConfigurationStatement(node);
                return true;
            case TsqlCreateAvailabilityGroupStatement node:
                VisitTsqlCreateAvailabilityGroupStatement(node);
                return true;
            case TsqlAlterAvailabilityGroupStatement node:
                VisitTsqlAlterAvailabilityGroupStatement(node);
                return true;
            case TsqlEndpointStatement node:
                VisitTsqlEndpointStatement(node);
                return true;
            case TsqlResourcePoolStatement node:
                VisitTsqlResourcePoolStatement(node);
                return true;
            case TsqlWorkloadGroupStatement node:
                VisitTsqlWorkloadGroupStatement(node);
                return true;
            case TsqlAlterResourceGovernorStatement node:
                VisitTsqlAlterResourceGovernorStatement(node);
                return true;
            case TsqlEventSessionStatement node:
                VisitTsqlEventSessionStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitTsqlAdminQuery2(Query query)
    {
        switch (query)
        {
            case TsqlMessageTypeStatement node:
                VisitTsqlMessageTypeStatement(node);
                return true;
            case TsqlCreateContractStatement node:
                VisitTsqlCreateContractStatement(node);
                return true;
            case TsqlCreateQueueStatement node:
                VisitTsqlCreateQueueStatement(node);
                return true;
            case TsqlAlterQueueStatement node:
                VisitTsqlAlterQueueStatement(node);
                return true;
            case TsqlServiceStatement node:
                VisitTsqlServiceStatement(node);
                return true;
            case TsqlRouteStatement node:
                VisitTsqlRouteStatement(node);
                return true;
            case TsqlBeginDialogStatement node:
                VisitTsqlBeginDialogStatement(node);
                return true;
            case TsqlBeginConversationTimerStatement node:
                VisitTsqlBeginConversationTimerStatement(node);
                return true;
            case TsqlSendStatement node:
                VisitTsqlSendStatement(node);
                return true;
            case TsqlReceiveStatement node:
                VisitTsqlReceiveStatement(node);
                return true;
            case TsqlEndConversationStatement node:
                VisitTsqlEndConversationStatement(node);
                return true;
            case TsqlGetConversationGroupStatement node:
                VisitTsqlGetConversationGroupStatement(node);
                return true;
            case TsqlMoveConversationStatement node:
                VisitTsqlMoveConversationStatement(node);
                return true;
            case TsqlCreateFulltextCatalogStatement node:
                VisitTsqlCreateFulltextCatalogStatement(node);
                return true;
            case TsqlAlterFulltextCatalogStatement node:
                VisitTsqlAlterFulltextCatalogStatement(node);
                return true;
            case TsqlCreateFulltextIndexStatement node:
                VisitTsqlCreateFulltextIndexStatement(node);
                return true;
            case TsqlCreateFulltextStoplistStatement node:
                VisitTsqlCreateFulltextStoplistStatement(node);
                return true;
            case TsqlAlterFulltextStoplistStatement node:
                VisitTsqlAlterFulltextStoplistStatement(node);
                return true;
            case TsqlCreateSearchPropertyListStatement node:
                VisitTsqlCreateSearchPropertyListStatement(node);
                return true;
            case TsqlAlterSearchPropertyListStatement node:
                VisitTsqlAlterSearchPropertyListStatement(node);
                return true;
            default:
                return false;
        }
    }

    public virtual void VisitTsqlDbccStatement(TsqlDbccStatement node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }

        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlBackupStatement(TsqlBackupStatement node)
    {
        VisitTsqlOptionList(node.Files);
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlRestoreStatement(TsqlRestoreStatement node)
    {
        VisitTsqlOptionList(node.Files);
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlCheckpointStatement(TsqlCheckpointStatement node)
    {
        return;
    }

    public virtual void VisitTsqlKillStatement(TsqlKillStatement node)
    {
        return;
    }

    public virtual void VisitTsqlReconfigureStatement(TsqlReconfigureStatement node)
    {
        return;
    }

    public virtual void VisitTsqlShutdownStatement(TsqlShutdownStatement node)
    {
        return;
    }

    public virtual void VisitTsqlSetUserStatement(TsqlSetUserStatement node)
    {
        return;
    }

    public virtual void VisitTsqlReadTextStatement(TsqlReadTextStatement node)
    {
        Visit(node.TextPointer);
        Visit(node.Offset);
        Visit(node.Size);
    }

    public virtual void VisitTsqlWriteTextStatement(TsqlWriteTextStatement node)
    {
        Visit(node.TextPointer);
        Visit(node.Data);
    }

    public virtual void VisitTsqlUpdateTextStatement(TsqlUpdateTextStatement node)
    {
        Visit(node.TextPointer);
        Visit(node.InsertOffset);
        Visit(node.DeleteLength);
        if (node.Data is not null)
        {
            Visit(node.Data);
        }

        if (node.SourceTextPointer is not null)
        {
            Visit(node.SourceTextPointer);
        }
    }

    public virtual void VisitTsqlAlterServerConfigurationStatement(TsqlAlterServerConfigurationStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlCreateAvailabilityGroupStatement(TsqlCreateAvailabilityGroupStatement node)
    {
        VisitTsqlOptionList(node.Options);
        foreach (var replica in node.Replicas)
        {
            VisitTsqlAvailabilityReplica(replica);
        }

        if (node.Listener is not null)
        {
            VisitTsqlAvailabilityReplica(node.Listener);
        }
    }

    public virtual void VisitTsqlAlterAvailabilityGroupStatement(TsqlAlterAvailabilityGroupStatement node)
    {
        VisitTsqlOptionList(node.Options);
        foreach (var target in node.Targets)
        {
            VisitTsqlAvailabilityReplica(target);
        }
    }

    public virtual void VisitTsqlEndpointStatement(TsqlEndpointStatement node)
    {
        VisitTsqlOptionList(node.ProtocolOptions);
        VisitTsqlOptionList(node.PayloadOptions);
    }

    public virtual void VisitTsqlResourcePoolStatement(TsqlResourcePoolStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlWorkloadGroupStatement(TsqlWorkloadGroupStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlAlterResourceGovernorStatement(TsqlAlterResourceGovernorStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlEventSessionStatement(TsqlEventSessionStatement node)
    {
        foreach (var item in node.Items)
        {
            VisitTsqlEventSessionItem(item);
        }

        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlMessageTypeStatement(TsqlMessageTypeStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateContractStatement(TsqlCreateContractStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateQueueStatement(TsqlCreateQueueStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlAlterQueueStatement(TsqlAlterQueueStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlServiceStatement(TsqlServiceStatement node)
    {
        return;
    }

    public virtual void VisitTsqlRouteStatement(TsqlRouteStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlBeginDialogStatement(TsqlBeginDialogStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlBeginConversationTimerStatement(TsqlBeginConversationTimerStatement node)
    {
        return;
    }

    public virtual void VisitTsqlSendStatement(TsqlSendStatement node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitTsqlReceiveStatement(TsqlReceiveStatement node)
    {
        if (node.Top is not null)
        {
            Visit(node.Top);
        }

        foreach (var column in node.Columns)
        {
            VisitTsqlReceiveColumn(column);
        }

        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitTsqlEndConversationStatement(TsqlEndConversationStatement node)
    {
        return;
    }

    public virtual void VisitTsqlGetConversationGroupStatement(TsqlGetConversationGroupStatement node)
    {
        return;
    }

    public virtual void VisitTsqlMoveConversationStatement(TsqlMoveConversationStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateFulltextCatalogStatement(TsqlCreateFulltextCatalogStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlAlterFulltextCatalogStatement(TsqlAlterFulltextCatalogStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlCreateFulltextIndexStatement(TsqlCreateFulltextIndexStatement node)
    {
        VisitTsqlOptionList(node.Storage);
        VisitTsqlOptionList(node.Options);
    }

    public virtual void VisitTsqlCreateFulltextStoplistStatement(TsqlCreateFulltextStoplistStatement node)
    {
        return;
    }

    public virtual void VisitTsqlAlterFulltextStoplistStatement(TsqlAlterFulltextStoplistStatement node)
    {
        return;
    }

    public virtual void VisitTsqlCreateSearchPropertyListStatement(TsqlCreateSearchPropertyListStatement node)
    {
        return;
    }

    public virtual void VisitTsqlAlterSearchPropertyListStatement(TsqlAlterSearchPropertyListStatement node)
    {
        VisitTsqlOptionList(node.Options);
    }

    // Option lists hold tokens only, and the walk reaches nested lists so subclasses see every option.
    public virtual void VisitTsqlOptionList(TsqlOptionList? node)
    {
        if (node is null)
        {
            return;
        }

        foreach (var option in node.Options)
        {
            VisitTsqlOption(option);
        }
    }

    public virtual void VisitTsqlOption(TsqlOption node) => VisitTsqlOptionList(node.Options);

    public virtual void VisitTsqlAvailabilityReplica(TsqlAvailabilityReplica node) => VisitTsqlOptionList(node.Options);

    public virtual void VisitTsqlEventSessionItem(TsqlEventSessionItem node)
    {
        VisitTsqlOptionList(node.Settings);
        foreach (var action in node.Actions)
        {
            Visit(action);
        }

        if (node.Predicate is not null)
        {
            Visit(node.Predicate);
        }
    }

    public virtual void VisitTsqlReceiveColumn(TsqlReceiveColumn node) => Visit(node.Expression);
}
