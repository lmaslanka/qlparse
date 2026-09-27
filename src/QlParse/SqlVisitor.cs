using System.Runtime.CompilerServices;

namespace QlParse;

public partial class SqlVisitor
{
    public virtual void Visit(Query query)
    {
        if (TryVisitQuery1(query))
        {
            return;
        }

        if (TryVisitQuery2(query))
        {
            return;
        }

        if (TryVisitQuery3(query))
        {
            return;
        }

        if (TryVisitQuery4(query))
        {
            return;
        }

        if (TryVisitQuery5(query))
        {
            return;
        }

        if (TryVisitQuery6(query))
        {
            return;
        }

        throw new SwitchExpressionException(query);
    }

    private bool TryVisitQuery1(Query query)
    {
        switch (query)
        {
            case ValuesQuery node:
                VisitValuesQuery(node);
                return true;
            case SetOperation node:
                VisitSetOperation(node);
                return true;
            case ParenQuery node:
                VisitParenQuery(node);
                return true;
            case WithQuery node:
                VisitWithQuery(node);
                return true;
            case SelectStatement node:
                VisitSelectStatement(node);
                return true;
            case InsertStatement node:
                VisitInsertStatement(node);
                return true;
            case UpdateStatement node:
                VisitUpdateStatement(node);
                return true;
            case DeleteStatement node:
                VisitDeleteStatement(node);
                return true;
            case MergeStatement node:
                VisitMergeStatement(node);
                return true;
            case TruncateStatement node:
                VisitTruncateStatement(node);
                return true;
            case SchemaDefinition node:
                VisitSchemaDefinition(node);
                return true;
            case AlterSchemaStatement node:
                VisitAlterSchemaStatement(node);
                return true;
            case DropSchemaStatement node:
                VisitDropSchemaStatement(node);
                return true;
            case CreateTableStatement node:
                VisitCreateTableStatement(node);
                return true;
            case AlterTableStatement node:
                VisitAlterTableStatement(node);
                return true;
            case DropTableStatement node:
                VisitDropTableStatement(node);
                return true;
            case CreateViewStatement node:
                VisitCreateViewStatement(node);
                return true;
            case AlterViewStatement node:
                VisitAlterViewStatement(node);
                return true;
            case DropViewStatement node:
                VisitDropViewStatement(node);
                return true;
            case CreateDomainStatement node:
                VisitCreateDomainStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitQuery2(Query query)
    {
        switch (query)
        {
            case AlterDomainStatement node:
                VisitAlterDomainStatement(node);
                return true;
            case DropDomainStatement node:
                VisitDropDomainStatement(node);
                return true;
            case CreateTypeStatement node:
                VisitCreateTypeStatement(node);
                return true;
            case DropTypeStatement node:
                VisitDropTypeStatement(node);
                return true;
            case CreateOrderingStatement node:
                VisitCreateOrderingStatement(node);
                return true;
            case CreateCastStatement node:
                VisitCreateCastStatement(node);
                return true;
            case CreateTransformStatement node:
                VisitCreateTransformStatement(node);
                return true;
            case CreateAssertionStatement node:
                VisitCreateAssertionStatement(node);
                return true;
            case DropAssertionStatement node:
                VisitDropAssertionStatement(node);
                return true;
            case CreateCharacterSetStatement node:
                VisitCreateCharacterSetStatement(node);
                return true;
            case DropCharacterSetStatement node:
                VisitDropCharacterSetStatement(node);
                return true;
            case CreateCollationStatement node:
                VisitCreateCollationStatement(node);
                return true;
            case DropCollationStatement node:
                VisitDropCollationStatement(node);
                return true;
            case CreateTranslationStatement node:
                VisitCreateTranslationStatement(node);
                return true;
            case DropTranslationStatement node:
                VisitDropTranslationStatement(node);
                return true;
            case CreateSequenceStatement node:
                VisitCreateSequenceStatement(node);
                return true;
            case DropSequenceStatement node:
                VisitDropSequenceStatement(node);
                return true;
            case CreateIndexStatement node:
                VisitCreateIndexStatement(node);
                return true;
            case AlterIndexStatement node:
                VisitAlterIndexStatement(node);
                return true;
            case DropIndexStatement node:
                VisitDropIndexStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitQuery3(Query query)
    {
        switch (query)
        {
            case CommentStatement node:
                VisitCommentStatement(node);
                return true;
            case GrantPrivilegeStatement node:
                VisitGrantPrivilegeStatement(node);
                return true;
            case GrantRoleStatement node:
                VisitGrantRoleStatement(node);
                return true;
            case RevokePrivilegeStatement node:
                VisitRevokePrivilegeStatement(node);
                return true;
            case RevokeRoleStatement node:
                VisitRevokeRoleStatement(node);
                return true;
            case CreateRoleStatement node:
                VisitCreateRoleStatement(node);
                return true;
            case DropRoleStatement node:
                VisitDropRoleStatement(node);
                return true;
            case SetRoleStatement node:
                VisitSetRoleStatement(node);
                return true;
            case StartTransactionStatement node:
                VisitStartTransactionStatement(node);
                return true;
            case SetTransactionStatement node:
                VisitSetTransactionStatement(node);
                return true;
            case CommitStatement node:
                VisitCommitStatement(node);
                return true;
            case RollbackStatement node:
                VisitRollbackStatement(node);
                return true;
            case SavepointStatement node:
                VisitSavepointStatement(node);
                return true;
            case ReleaseSavepointStatement node:
                VisitReleaseSavepointStatement(node);
                return true;
            case SetConstraintsStatement node:
                VisitSetConstraintsStatement(node);
                return true;
            case SetSessionAuthorizationStatement node:
                VisitSetSessionAuthorizationStatement(node);
                return true;
            case SetSessionCharacteristicsStatement node:
                VisitSetSessionCharacteristicsStatement(node);
                return true;
            case SetNamesStatement node:
                VisitSetNamesStatement(node);
                return true;
            case SetCharacterSetStatement node:
                VisitSetCharacterSetStatement(node);
                return true;
            case SetCollationStatement node:
                VisitSetCollationStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitQuery4(Query query)
    {
        switch (query)
        {
            case SetTimeZoneStatement node:
                VisitSetTimeZoneStatement(node);
                return true;
            case SetCatalogStatement node:
                VisitSetCatalogStatement(node);
                return true;
            case SetSchemaStatement node:
                VisitSetSchemaStatement(node);
                return true;
            case SetPathStatement node:
                VisitSetPathStatement(node);
                return true;
            case ConnectStatement node:
                VisitConnectStatement(node);
                return true;
            case DisconnectStatement node:
                VisitDisconnectStatement(node);
                return true;
            case SetConnectionStatement node:
                VisitSetConnectionStatement(node);
                return true;
            case DeclareCursorStatement node:
                VisitDeclareCursorStatement(node);
                return true;
            case OpenStatement node:
                VisitOpenStatement(node);
                return true;
            case FetchStatement node:
                VisitFetchStatement(node);
                return true;
            case CloseStatement node:
                VisitCloseStatement(node);
                return true;
            case AllocateCursorStatement node:
                VisitAllocateCursorStatement(node);
                return true;
            case DeallocateStatement node:
                VisitDeallocateStatement(node);
                return true;
            case PrepareStatement node:
                VisitPrepareStatement(node);
                return true;
            case ExecuteStatement node:
                VisitExecuteStatement(node);
                return true;
            case ExecuteImmediateStatement node:
                VisitExecuteImmediateStatement(node);
                return true;
            case DescribeStatement node:
                VisitDescribeStatement(node);
                return true;
            case DynamicDeclareCursorStatement node:
                VisitDynamicDeclareCursorStatement(node);
                return true;
            case AllocateDescriptorStatement node:
                VisitAllocateDescriptorStatement(node);
                return true;
            case GetDiagnosticsStatement node:
                VisitGetDiagnosticsStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitQuery5(Query query)
    {
        switch (query)
        {
            case SignalStatement node:
                VisitSignalStatement(node);
                return true;
            case ResignalStatement node:
                VisitResignalStatement(node);
                return true;
            case CreateTriggerStatement node:
                VisitCreateTriggerStatement(node);
                return true;
            case DropTriggerStatement node:
                VisitDropTriggerStatement(node);
                return true;
            case CreateRoutineStatement node:
                VisitCreateRoutineStatement(node);
                return true;
            case AlterRoutineStatement node:
                VisitAlterRoutineStatement(node);
                return true;
            case DropRoutineStatement node:
                VisitDropRoutineStatement(node);
                return true;
            case CallStatement node:
                VisitCallStatement(node);
                return true;
            case ReturnStatement node:
                VisitReturnStatement(node);
                return true;
            case CompoundStatement node:
                VisitCompoundStatement(node);
                return true;
            case DeclareVariableStatement node:
                VisitDeclareVariableStatement(node);
                return true;
            case DeclareConditionStatement node:
                VisitDeclareConditionStatement(node);
                return true;
            case DeclareHandlerStatement node:
                VisitDeclareHandlerStatement(node);
                return true;
            case SetAssignmentStatement node:
                VisitSetAssignmentStatement(node);
                return true;
            case IfStatement node:
                VisitIfStatement(node);
                return true;
            case CaseStatement node:
                VisitCaseStatement(node);
                return true;
            case LoopStatement node:
                VisitLoopStatement(node);
                return true;
            case WhileStatement node:
                VisitWhileStatement(node);
                return true;
            case RepeatStatement node:
                VisitRepeatStatement(node);
                return true;
            case ForStatement node:
                VisitForStatement(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitQuery6(Query query)
    {
        switch (query)
        {
            case LeaveStatement node:
                VisitLeaveStatement(node);
                return true;
            case IterateStatement node:
                VisitIterateStatement(node);
                return true;
            case CreatePropertyGraphStatement node:
                VisitCreatePropertyGraphStatement(node);
                return true;
            case DropPropertyGraphStatement node:
                VisitDropPropertyGraphStatement(node);
                return true;
            case DirectSqlScript node:
                VisitDirectSqlScript(node);
                return true;
            case ModuleDefinition node:
                VisitModuleDefinition(node);
                return true;
            case ModuleProcedure node:
                VisitModuleProcedure(node);
                return true;
            case EmbeddedSqlStatement node:
                VisitEmbeddedSqlStatement(node);
                return true;
            case DeclareSectionStatement node:
                VisitDeclareSectionStatement(node);
                return true;
            case WheneverStatement node:
                VisitWheneverStatement(node);
                return true;
            default:
                return false;
        }
    }


    public virtual void Visit(Expression expression)
    {
        if (TryVisitExpression1(expression))
        {
            return;
        }

        if (TryVisitExpression2(expression))
        {
            return;
        }

        if (TryVisitExpression3(expression))
        {
            return;
        }

        if (TryVisitExpression4(expression))
        {
            return;
        }

        throw new SwitchExpressionException(expression);
    }

    private bool TryVisitExpression1(Expression expression)
    {
        switch (expression)
        {
            case CastExpression node:
                VisitCastExpression(node);
                return true;
            case TreatExpression node:
                VisitTreatExpression(node);
                return true;
            case DerefExpression node:
                VisitDerefExpression(node);
                return true;
            case RefValueExpression node:
                VisitRefValueExpression(node);
                return true;
            case DereferenceExpression node:
                VisitDereferenceExpression(node);
                return true;
            case SpecifictypeExpression node:
                VisitSpecifictypeExpression(node);
                return true;
            case MethodInvocationExpression node:
                VisitMethodInvocationExpression(node);
                return true;
            case StaticMethodInvocationExpression node:
                VisitStaticMethodInvocationExpression(node);
                return true;
            case NewSpecificationExpression node:
                VisitNewSpecificationExpression(node);
                return true;
            case NullIfExpression node:
                VisitNullIfExpression(node);
                return true;
            case CoalesceExpression node:
                VisitCoalesceExpression(node);
                return true;
            case NextValueExpression node:
                VisitNextValueExpression(node);
                return true;
            case ColonCastExpression node:
                VisitColonCastExpression(node);
                return true;
            case RowConstructorExpression node:
                VisitRowConstructorExpression(node);
                return true;
            case MatchExpression node:
                VisitMatchExpression(node);
                return true;
            case OverlapsExpression node:
                VisitOverlapsExpression(node);
                return true;
            case CollateExpression node:
                VisitCollateExpression(node);
                return true;
            case ArrayExpression node:
                VisitArrayExpression(node);
                return true;
            case ArrayQueryExpression node:
                VisitArrayQueryExpression(node);
                return true;
            case MultisetExpression node:
                VisitMultisetExpression(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitExpression2(Expression expression)
    {
        switch (expression)
        {
            case MultisetQueryExpression node:
                VisitMultisetQueryExpression(node);
                return true;
            case MultisetOperationExpression node:
                VisitMultisetOperationExpression(node);
                return true;
            case MultisetSetExpression node:
                VisitMultisetSetExpression(node);
                return true;
            case AbsentOnNullExpression node:
                VisitAbsentOnNullExpression(node);
                return true;
            case IdentifierExpression node:
                VisitIdentifierExpression(node);
                return true;
            case HostParameterExpression node:
                VisitHostParameterExpression(node);
                return true;
            case EmbeddedHostExpression node:
                VisitEmbeddedHostExpression(node);
                return true;
            case LiteralExpression node:
                VisitLiteralExpression(node);
                return true;
            case NiladicFunctionExpression node:
                VisitNiladicFunctionExpression(node);
                return true;
            case DatetimeLiteralExpression node:
                VisitDatetimeLiteralExpression(node);
                return true;
            case IntervalLiteralExpression node:
                VisitIntervalLiteralExpression(node);
                return true;
            case TrimExpression node:
                VisitTrimExpression(node);
                return true;
            case ExtractExpression node:
                VisitExtractExpression(node);
                return true;
            case SubstringExpression node:
                VisitSubstringExpression(node);
                return true;
            case UsingTransformExpression node:
                VisitUsingTransformExpression(node);
                return true;
            case PositionExpression node:
                VisitPositionExpression(node);
                return true;
            case SpecialFormExpression node:
                VisitSpecialFormExpression(node);
                return true;
            case GroupingOperationExpression node:
                VisitGroupingOperationExpression(node);
                return true;
            case EmptyGroupingSetExpression node:
                VisitEmptyGroupingSetExpression(node);
                return true;
            case OverlayExpression node:
                VisitOverlayExpression(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitExpression3(Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression node:
                VisitBinaryExpression(node);
                return true;
            case MemberAccessExpression node:
                VisitMemberAccessExpression(node);
                return true;
            case BetweenExpression node:
                VisitBetweenExpression(node);
                return true;
            case InExpression node:
                VisitInExpression(node);
                return true;
            case LikeExpression node:
                VisitLikeExpression(node);
                return true;
            case SimilarExpression node:
                VisitSimilarExpression(node);
                return true;
            case DistinctFromExpression node:
                VisitDistinctFromExpression(node);
                return true;
            case NormalizedPredicateExpression node:
                VisitNormalizedPredicateExpression(node);
                return true;
            case IsExpression node:
                VisitIsExpression(node);
                return true;
            case UnaryExpression node:
                VisitUnaryExpression(node);
                return true;
            case NotExpression node:
                VisitNotExpression(node);
                return true;
            case FunctionCallExpression node:
                VisitFunctionCallExpression(node);
                return true;
            case ParenExpression node:
                VisitParenExpression(node);
                return true;
            case ScalarSubqueryExpression node:
                VisitScalarSubqueryExpression(node);
                return true;
            case ExistsExpression node:
                VisitExistsExpression(node);
                return true;
            case UniqueExpression node:
                VisitUniqueExpression(node);
                return true;
            case QuantifiedSubqueryExpression node:
                VisitQuantifiedSubqueryExpression(node);
                return true;
            case CaseExpression node:
                VisitCaseExpression(node);
                return true;
            case StarExpression node:
                VisitStarExpression(node);
                return true;
            case QualifiedStarExpression node:
                VisitQualifiedStarExpression(node);
                return true;
            default:
                return false;
        }
    }

    private bool TryVisitExpression4(Expression expression)
    {
        switch (expression)
        {
            case PeriodExpression node:
                VisitPeriodExpression(node);
                return true;
            case PeriodPredicateExpression node:
                VisitPeriodPredicateExpression(node);
                return true;
            case JsonAccessorExpression node:
                VisitJsonAccessorExpression(node);
                return true;
            case MarkupCallExpression node:
                VisitMarkupCallExpression(node);
                return true;
            case MdarrayConstructorExpression node:
                VisitMdarrayConstructorExpression(node);
                return true;
            case MdarraySliceExpression node:
                VisitMdarraySliceExpression(node);
                return true;
            case MdarrayAggregateExpression node:
                VisitMdarrayAggregateExpression(node);
                return true;
            default:
                return false;
        }
    }


}
