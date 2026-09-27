using System.Runtime.CompilerServices;

namespace QlParse;

public partial class SqlVisitor
{
    public virtual void VisitCommentStatement(CommentStatement node)
    {
        return;
    }

    public virtual void VisitGrantPrivilegeStatement(GrantPrivilegeStatement node)
    {
        return;
    }

    public virtual void VisitGrantRoleStatement(GrantRoleStatement node)
    {
        return;
    }

    public virtual void VisitRevokePrivilegeStatement(RevokePrivilegeStatement node)
    {
        return;
    }

    public virtual void VisitRevokeRoleStatement(RevokeRoleStatement node)
    {
        return;
    }

    public virtual void VisitCreateRoleStatement(CreateRoleStatement node)
    {
        return;
    }

    public virtual void VisitDropRoleStatement(DropRoleStatement node)
    {
        return;
    }

    public virtual void VisitSetRoleStatement(SetRoleStatement node)
    {
        return;
    }

    public virtual void VisitStartTransactionStatement(StartTransactionStatement node) => VisitTransactionModes(node.Modes);

    public virtual void VisitSetTransactionStatement(SetTransactionStatement node) => VisitTransactionModes(node.Modes);

    public virtual void VisitTransactionModes(IReadOnlyList<TransactionMode> modes)
    {
        foreach (var mode in modes)
        {
            VisitTransactionMode(mode);
        }
    }

    public virtual void VisitTransactionMode(TransactionMode node)
    {
        if (node.Size is not null)
        {
            Visit(node.Size);
        }
    }

    public virtual void VisitCommitStatement(CommitStatement node)
    {
        return;
    }

    public virtual void VisitRollbackStatement(RollbackStatement node)
    {
        return;
    }

    public virtual void VisitSavepointStatement(SavepointStatement node)
    {
        return;
    }

    public virtual void VisitReleaseSavepointStatement(ReleaseSavepointStatement node)
    {
        return;
    }

    public virtual void VisitSetConstraintsStatement(SetConstraintsStatement node)
    {
        return;
    }

    public virtual void VisitSetSessionAuthorizationStatement(SetSessionAuthorizationStatement node)
    {
        return;
    }

    public virtual void VisitSetSessionCharacteristicsStatement(SetSessionCharacteristicsStatement node) =>
        VisitTransactionModes(node.Modes);

    public virtual void VisitSetNamesStatement(SetNamesStatement node)
    {
        return;
    }

    public virtual void VisitSetCharacterSetStatement(SetCharacterSetStatement node)
    {
        return;
    }

    public virtual void VisitSetCollationStatement(SetCollationStatement node)
    {
        return;
    }

    public virtual void VisitSetTimeZoneStatement(SetTimeZoneStatement node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitSetCatalogStatement(SetCatalogStatement node)
    {
        return;
    }

    public virtual void VisitSetSchemaStatement(SetSchemaStatement node)
    {
        return;
    }

    public virtual void VisitSetPathStatement(SetPathStatement node)
    {
        return;
    }

    public virtual void VisitConnectStatement(ConnectStatement node)
    {
        return;
    }

    public virtual void VisitDisconnectStatement(DisconnectStatement node)
    {
        return;
    }

    public virtual void VisitSetConnectionStatement(SetConnectionStatement node)
    {
        return;
    }

    public virtual void VisitDeclareCursorStatement(DeclareCursorStatement node) => Visit(node.Query);

    public virtual void VisitOpenStatement(OpenStatement node)
    {
        return;
    }

    public virtual void VisitFetchStatement(FetchStatement node)
    {
        if (node.Offset is not null)
        {
            Visit(node.Offset);
        }
    }

    public virtual void VisitCloseStatement(CloseStatement node)
    {
        return;
    }

    public virtual void VisitAllocateCursorStatement(AllocateCursorStatement node)
    {
        return;
    }

    public virtual void VisitDeallocateStatement(DeallocateStatement node)
    {
        return;
    }

    public virtual void VisitPrepareStatement(PrepareStatement node)
    {
        return;
    }

    public virtual void VisitExecuteStatement(ExecuteStatement node)
    {
        return;
    }

    public virtual void VisitExecuteImmediateStatement(ExecuteImmediateStatement node)
    {
        return;
    }

    public virtual void VisitDescribeStatement(DescribeStatement node)
    {
        return;
    }

    public virtual void VisitDynamicDeclareCursorStatement(DynamicDeclareCursorStatement node)
    {
        return;
    }

    public virtual void VisitAllocateDescriptorStatement(AllocateDescriptorStatement node)
    {
        return;
    }

    public virtual void VisitGetDiagnosticsStatement(GetDiagnosticsStatement node)
    {
        if (node.ConditionNumber is not null)
        {
            Visit(node.ConditionNumber);
        }
    }

    public virtual void VisitSignalStatement(SignalStatement node) => VisitSignalInformation(node.Items);

    public virtual void VisitResignalStatement(ResignalStatement node) => VisitSignalInformation(node.Items);

    public virtual void VisitCompoundStatement(CompoundStatement node) => VisitAllQueries(node.Statements);

    public virtual void VisitDeclareVariableStatement(DeclareVariableStatement node)
    {
        VisitDataType(node.Type);
        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }
    }

    public virtual void VisitDeclareConditionStatement(DeclareConditionStatement node)
    {
        return;
    }

    public virtual void VisitDeclareHandlerStatement(DeclareHandlerStatement node) => Visit(node.Action);

    public virtual void VisitSetAssignmentStatement(SetAssignmentStatement node) => Visit(node.Value);

    public virtual void VisitIfStatement(IfStatement node)
    {
        Visit(node.Condition);
        VisitAllQueries(node.ThenStatements);
        foreach (var elseIf in node.ElseIfs)
        {
            Visit(elseIf.Condition);
            VisitAllQueries(elseIf.Statements);
        }

        VisitAllQueries(node.ElseStatements);
    }

    public virtual void VisitCaseStatement(CaseStatement node)
    {
        if (node.Operand is not null)
        {
            Visit(node.Operand);
        }

        foreach (var when in node.Whens)
        {
            Visit(when.Operand);
            VisitAllQueries(when.Statements);
        }

        VisitAllQueries(node.ElseStatements);
    }

    public virtual void VisitLoopStatement(LoopStatement node) => VisitAllQueries(node.Statements);

    public virtual void VisitWhileStatement(WhileStatement node)
    {
        Visit(node.Condition);
        VisitAllQueries(node.Statements);
    }

    public virtual void VisitRepeatStatement(RepeatStatement node)
    {
        VisitAllQueries(node.Statements);
        Visit(node.Condition);
    }

    public virtual void VisitForStatement(ForStatement node)
    {
        Visit(node.CursorQuery);
        VisitAllQueries(node.Statements);
    }

    public virtual void VisitLeaveStatement(LeaveStatement node)
    {
        return;
    }

    public virtual void VisitIterateStatement(IterateStatement node)
    {
        return;
    }

    private void VisitAllQueries(IReadOnlyList<Query> queries)
    {
        foreach (var query in queries)
        {
            Visit(query);
        }
    }

    public virtual void VisitCreateTriggerStatement(CreateTriggerStatement node)
    {
        if (node.When is not null)
        {
            Visit(node.When);
        }

        Visit(node.Body);
    }

    public virtual void VisitDropTriggerStatement(DropTriggerStatement node)
    {
        return;
    }

    public virtual void VisitCreateRoutineStatement(CreateRoutineStatement node)
    {
        foreach (var parameter in node.Parameters)
        {
            VisitRoutineParameter(parameter);
        }

        if (node.ReturnsType is not null)
        {
            VisitDataType(node.ReturnsType);
        }

        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitAlterRoutineStatement(AlterRoutineStatement node)
    {
        if (node.Body is not null)
        {
            Visit(node.Body);
        }
    }

    public virtual void VisitDropRoutineStatement(DropRoutineStatement node)
    {
        return;
    }

    public virtual void VisitCallStatement(CallStatement node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
    }

    public virtual void VisitReturnStatement(ReturnStatement node) => Visit(node.Value);

    public virtual void VisitSignalInformation(IReadOnlyList<SignalInformation> items)
    {
        foreach (var item in items)
        {
            Visit(item.Value);
        }
    }

    public virtual void Visit(AlterTableAction action)
    {
        switch (action)
        {
            case AddColumnAction node:
                VisitAddColumnAction(node);
                break;
            case DropColumnAction node:
                VisitDropColumnAction(node);
                break;
            case AddConstraintAction node:
                VisitAddConstraintAction(node);
                break;
            case DropConstraintAction node:
                VisitDropConstraintAction(node);
                break;
            case AlterColumnAction node:
                VisitAlterColumnAction(node);
                break;
            case AddPeriodAction node:
                VisitAddPeriodAction(node);
                break;
            case DropPeriodAction node:
                VisitDropPeriodAction(node);
                break;
            case SystemVersioningAction node:
                VisitSystemVersioningAction(node);
                break;
            default:
                throw new SwitchExpressionException(action);
        }
    }

    public virtual void VisitAddColumnAction(AddColumnAction node) => VisitColumnDefinition(node.Column);

    public virtual void VisitDropColumnAction(DropColumnAction node)
    {
        return;
    }

    public virtual void VisitAddConstraintAction(AddConstraintAction node) => VisitTableConstraint(node.Constraint);

    public virtual void VisitDropConstraintAction(DropConstraintAction node)
    {
        return;
    }

    public virtual void VisitAddPeriodAction(AddPeriodAction node) => VisitPeriodDefinition(node.Period);

    public virtual void VisitDropPeriodAction(DropPeriodAction node)
    {
        return;
    }

    public virtual void VisitSystemVersioningAction(SystemVersioningAction node)
    {
        return;
    }

    public virtual void VisitAlterColumnAction(AlterColumnAction node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }

        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }

        if (node.RestartValue is not null)
        {
            Visit(node.RestartValue);
        }

        if (node.IdentityOption is not null)
        {
            VisitSequenceOption(node.IdentityOption);
        }
    }

    public virtual void VisitMergeWhenClause(MergeWhenClause node)
    {
        if (node.Condition is not null)
        {
            Visit(node.Condition);
        }

        Visit(node.Action);
    }

    public virtual void Visit(MergeAction action)
    {
        switch (action)
        {
            case MergeUpdateAction node:
                VisitMergeUpdateAction(node);
                break;
            case MergeDeleteAction node:
                VisitMergeDeleteAction(node);
                break;
            case MergeInsertAction node:
                VisitMergeInsertAction(node);
                break;
            default:
                throw new SwitchExpressionException(action);
        }
    }

    public virtual void VisitMergeUpdateAction(MergeUpdateAction node) => VisitSetClauses(node.Assignments);

    public virtual void VisitMergeDeleteAction(MergeDeleteAction node)
    {
        return;
    }

    public virtual void VisitMergeInsertAction(MergeInsertAction node) => VisitAll(node.Values);

    public virtual void VisitSetClause(SetClause node)
    {
        foreach (var target in node.Targets)
        {
            VisitSetTarget(target);
        }

        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitSetTarget(SetTarget node)
    {
        if (node.Index is not null)
        {
            Visit(node.Index);
        }
    }

    private void VisitSetClauses(IReadOnlyList<SetClause> clauses)
    {
        foreach (var clause in clauses)
        {
            VisitSetClause(clause);
        }
    }

}
