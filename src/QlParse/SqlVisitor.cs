namespace QlParse;

public abstract class SqlVisitor
{
    public virtual void Visit(Query query)
    {
        switch (query)
        {
            case ValuesQuery node:
                VisitValuesQuery(node);
                break;
            case SetOperation node:
                VisitSetOperation(node);
                break;
            case ParenQuery node:
                VisitParenQuery(node);
                break;
            case WithQuery node:
                VisitWithQuery(node);
                break;
            case SelectStatement node:
                VisitSelectStatement(node);
                break;
            case InsertStatement node:
                VisitInsertStatement(node);
                break;
            case UpdateStatement node:
                VisitUpdateStatement(node);
                break;
            case DeleteStatement node:
                VisitDeleteStatement(node);
                break;
            case MergeStatement node:
                VisitMergeStatement(node);
                break;
            case TruncateStatement node:
                VisitTruncateStatement(node);
                break;
            case SchemaDefinition node:
                VisitSchemaDefinition(node);
                break;
            case AlterSchemaStatement node:
                VisitAlterSchemaStatement(node);
                break;
            case DropSchemaStatement node:
                VisitDropSchemaStatement(node);
                break;
            case CreateTableStatement node:
                VisitCreateTableStatement(node);
                break;
            case AlterTableStatement node:
                VisitAlterTableStatement(node);
                break;
            case DropTableStatement node:
                VisitDropTableStatement(node);
                break;
            case CreateViewStatement node:
                VisitCreateViewStatement(node);
                break;
            case AlterViewStatement node:
                VisitAlterViewStatement(node);
                break;
            case DropViewStatement node:
                VisitDropViewStatement(node);
                break;
            case CreateDomainStatement node:
                VisitCreateDomainStatement(node);
                break;
            case AlterDomainStatement node:
                VisitAlterDomainStatement(node);
                break;
            case DropDomainStatement node:
                VisitDropDomainStatement(node);
                break;
            case CreateTypeStatement node:
                VisitCreateTypeStatement(node);
                break;
            case DropTypeStatement node:
                VisitDropTypeStatement(node);
                break;
            case CreateOrderingStatement node:
                VisitCreateOrderingStatement(node);
                break;
            case CreateCastStatement node:
                VisitCreateCastStatement(node);
                break;
            case CreateTransformStatement node:
                VisitCreateTransformStatement(node);
                break;
            case CreateAssertionStatement node:
                VisitCreateAssertionStatement(node);
                break;
            case DropAssertionStatement node:
                VisitDropAssertionStatement(node);
                break;
            case CreateCharacterSetStatement node:
                VisitCreateCharacterSetStatement(node);
                break;
            case DropCharacterSetStatement node:
                VisitDropCharacterSetStatement(node);
                break;
            case CreateCollationStatement node:
                VisitCreateCollationStatement(node);
                break;
            case DropCollationStatement node:
                VisitDropCollationStatement(node);
                break;
            case CreateTranslationStatement node:
                VisitCreateTranslationStatement(node);
                break;
            case DropTranslationStatement node:
                VisitDropTranslationStatement(node);
                break;
            case CreateSequenceStatement node:
                VisitCreateSequenceStatement(node);
                break;
            case DropSequenceStatement node:
                VisitDropSequenceStatement(node);
                break;
            case CreateIndexStatement node:
                VisitCreateIndexStatement(node);
                break;
            case AlterIndexStatement node:
                VisitAlterIndexStatement(node);
                break;
            case DropIndexStatement node:
                VisitDropIndexStatement(node);
                break;
            case CommentStatement node:
                VisitCommentStatement(node);
                break;
            case GrantPrivilegeStatement node:
                VisitGrantPrivilegeStatement(node);
                break;
            case GrantRoleStatement node:
                VisitGrantRoleStatement(node);
                break;
            case RevokePrivilegeStatement node:
                VisitRevokePrivilegeStatement(node);
                break;
            case RevokeRoleStatement node:
                VisitRevokeRoleStatement(node);
                break;
            case CreateRoleStatement node:
                VisitCreateRoleStatement(node);
                break;
            case DropRoleStatement node:
                VisitDropRoleStatement(node);
                break;
            case SetRoleStatement node:
                VisitSetRoleStatement(node);
                break;
            case StartTransactionStatement node:
                VisitStartTransactionStatement(node);
                break;
            case SetTransactionStatement node:
                VisitSetTransactionStatement(node);
                break;
            case CommitStatement node:
                VisitCommitStatement(node);
                break;
            case RollbackStatement node:
                VisitRollbackStatement(node);
                break;
            case SavepointStatement node:
                VisitSavepointStatement(node);
                break;
            case ReleaseSavepointStatement node:
                VisitReleaseSavepointStatement(node);
                break;
            case SetConstraintsStatement node:
                VisitSetConstraintsStatement(node);
                break;
            case SetSessionAuthorizationStatement node:
                VisitSetSessionAuthorizationStatement(node);
                break;
            case SetSessionCharacteristicsStatement node:
                VisitSetSessionCharacteristicsStatement(node);
                break;
            case SetNamesStatement node:
                VisitSetNamesStatement(node);
                break;
            case SetCharacterSetStatement node:
                VisitSetCharacterSetStatement(node);
                break;
            case SetCollationStatement node:
                VisitSetCollationStatement(node);
                break;
            case SetTimeZoneStatement node:
                VisitSetTimeZoneStatement(node);
                break;
            case SetCatalogStatement node:
                VisitSetCatalogStatement(node);
                break;
            case SetSchemaStatement node:
                VisitSetSchemaStatement(node);
                break;
            case SetPathStatement node:
                VisitSetPathStatement(node);
                break;
            case ConnectStatement node:
                VisitConnectStatement(node);
                break;
            case DisconnectStatement node:
                VisitDisconnectStatement(node);
                break;
            case SetConnectionStatement node:
                VisitSetConnectionStatement(node);
                break;
            case DeclareCursorStatement node:
                VisitDeclareCursorStatement(node);
                break;
            case OpenStatement node:
                VisitOpenStatement(node);
                break;
            case FetchStatement node:
                VisitFetchStatement(node);
                break;
            case CloseStatement node:
                VisitCloseStatement(node);
                break;
            case AllocateCursorStatement node:
                VisitAllocateCursorStatement(node);
                break;
            case DeallocateStatement node:
                VisitDeallocateStatement(node);
                break;
            case PrepareStatement node:
                VisitPrepareStatement(node);
                break;
            case ExecuteStatement node:
                VisitExecuteStatement(node);
                break;
            case ExecuteImmediateStatement node:
                VisitExecuteImmediateStatement(node);
                break;
            case DescribeStatement node:
                VisitDescribeStatement(node);
                break;
            case DynamicDeclareCursorStatement node:
                VisitDynamicDeclareCursorStatement(node);
                break;
            case AllocateDescriptorStatement node:
                VisitAllocateDescriptorStatement(node);
                break;
            case GetDiagnosticsStatement node:
                VisitGetDiagnosticsStatement(node);
                break;
            case SignalStatement node:
                VisitSignalStatement(node);
                break;
            case ResignalStatement node:
                VisitResignalStatement(node);
                break;
            case CreateTriggerStatement node:
                VisitCreateTriggerStatement(node);
                break;
            case DropTriggerStatement node:
                VisitDropTriggerStatement(node);
                break;
            case CreateRoutineStatement node:
                VisitCreateRoutineStatement(node);
                break;
            case AlterRoutineStatement node:
                VisitAlterRoutineStatement(node);
                break;
            case DropRoutineStatement node:
                VisitDropRoutineStatement(node);
                break;
            case CallStatement node:
                VisitCallStatement(node);
                break;
            case ReturnStatement node:
                VisitReturnStatement(node);
                break;
            case CompoundStatement node:
                VisitCompoundStatement(node);
                break;
            case DeclareVariableStatement node:
                VisitDeclareVariableStatement(node);
                break;
            case DeclareConditionStatement node:
                VisitDeclareConditionStatement(node);
                break;
            case DeclareHandlerStatement node:
                VisitDeclareHandlerStatement(node);
                break;
            case SetAssignmentStatement node:
                VisitSetAssignmentStatement(node);
                break;
            case IfStatement node:
                VisitIfStatement(node);
                break;
            case CaseStatement node:
                VisitCaseStatement(node);
                break;
            case LoopStatement node:
                VisitLoopStatement(node);
                break;
            case WhileStatement node:
                VisitWhileStatement(node);
                break;
            case RepeatStatement node:
                VisitRepeatStatement(node);
                break;
            case ForStatement node:
                VisitForStatement(node);
                break;
            case LeaveStatement node:
                VisitLeaveStatement(node);
                break;
            case IterateStatement node:
                VisitIterateStatement(node);
                break;
            case CreatePropertyGraphStatement node:
                VisitCreatePropertyGraphStatement(node);
                break;
            case DropPropertyGraphStatement node:
                VisitDropPropertyGraphStatement(node);
                break;
            case DirectSqlScript node:
                VisitDirectSqlScript(node);
                break;
            case ModuleDefinition node:
                VisitModuleDefinition(node);
                break;
            case ModuleProcedure node:
                VisitModuleProcedure(node);
                break;
            case EmbeddedSqlStatement node:
                VisitEmbeddedSqlStatement(node);
                break;
            case DeclareSectionStatement node:
                VisitDeclareSectionStatement(node);
                break;
            case WheneverStatement node:
                VisitWheneverStatement(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown query {query.GetType()}");
        }
    }

    public virtual void Visit(Expression expression)
    {
        switch (expression)
        {
            case CastExpression node:
                VisitCastExpression(node);
                break;
            case TreatExpression node:
                VisitTreatExpression(node);
                break;
            case DerefExpression node:
                VisitDerefExpression(node);
                break;
            case RefValueExpression node:
                VisitRefValueExpression(node);
                break;
            case DereferenceExpression node:
                VisitDereferenceExpression(node);
                break;
            case SpecifictypeExpression node:
                VisitSpecifictypeExpression(node);
                break;
            case MethodInvocationExpression node:
                VisitMethodInvocationExpression(node);
                break;
            case StaticMethodInvocationExpression node:
                VisitStaticMethodInvocationExpression(node);
                break;
            case NewSpecificationExpression node:
                VisitNewSpecificationExpression(node);
                break;
            case NullIfExpression node:
                VisitNullIfExpression(node);
                break;
            case CoalesceExpression node:
                VisitCoalesceExpression(node);
                break;
            case NextValueExpression node:
                VisitNextValueExpression(node);
                break;
            case ColonCastExpression node:
                VisitColonCastExpression(node);
                break;
            case RowConstructorExpression node:
                VisitRowConstructorExpression(node);
                break;
            case MatchExpression node:
                VisitMatchExpression(node);
                break;
            case OverlapsExpression node:
                VisitOverlapsExpression(node);
                break;
            case CollateExpression node:
                VisitCollateExpression(node);
                break;
            case ArrayExpression node:
                VisitArrayExpression(node);
                break;
            case ArrayQueryExpression node:
                VisitArrayQueryExpression(node);
                break;
            case MultisetExpression node:
                VisitMultisetExpression(node);
                break;
            case MultisetQueryExpression node:
                VisitMultisetQueryExpression(node);
                break;
            case MultisetOperationExpression node:
                VisitMultisetOperationExpression(node);
                break;
            case MultisetSetExpression node:
                VisitMultisetSetExpression(node);
                break;
            case AbsentOnNullExpression node:
                VisitAbsentOnNullExpression(node);
                break;
            case IdentifierExpression node:
                VisitIdentifierExpression(node);
                break;
            case HostParameterExpression node:
                VisitHostParameterExpression(node);
                break;
            case EmbeddedHostExpression node:
                VisitEmbeddedHostExpression(node);
                break;
            case LiteralExpression node:
                VisitLiteralExpression(node);
                break;
            case NiladicFunctionExpression node:
                VisitNiladicFunctionExpression(node);
                break;
            case DatetimeLiteralExpression node:
                VisitDatetimeLiteralExpression(node);
                break;
            case IntervalLiteralExpression node:
                VisitIntervalLiteralExpression(node);
                break;
            case TrimExpression node:
                VisitTrimExpression(node);
                break;
            case ExtractExpression node:
                VisitExtractExpression(node);
                break;
            case SubstringExpression node:
                VisitSubstringExpression(node);
                break;
            case UsingTransformExpression node:
                VisitUsingTransformExpression(node);
                break;
            case PositionExpression node:
                VisitPositionExpression(node);
                break;
            case SpecialFormExpression node:
                VisitSpecialFormExpression(node);
                break;
            case GroupingOperationExpression node:
                VisitGroupingOperationExpression(node);
                break;
            case EmptyGroupingSetExpression node:
                VisitEmptyGroupingSetExpression(node);
                break;
            case OverlayExpression node:
                VisitOverlayExpression(node);
                break;
            case BinaryExpression node:
                VisitBinaryExpression(node);
                break;
            case MemberAccessExpression node:
                VisitMemberAccessExpression(node);
                break;
            case BetweenExpression node:
                VisitBetweenExpression(node);
                break;
            case InExpression node:
                VisitInExpression(node);
                break;
            case LikeExpression node:
                VisitLikeExpression(node);
                break;
            case SimilarExpression node:
                VisitSimilarExpression(node);
                break;
            case DistinctFromExpression node:
                VisitDistinctFromExpression(node);
                break;
            case NormalizedPredicateExpression node:
                VisitNormalizedPredicateExpression(node);
                break;
            case IsExpression node:
                VisitIsExpression(node);
                break;
            case UnaryExpression node:
                VisitUnaryExpression(node);
                break;
            case NotExpression node:
                VisitNotExpression(node);
                break;
            case FunctionCallExpression node:
                VisitFunctionCallExpression(node);
                break;
            case ParenExpression node:
                VisitParenExpression(node);
                break;
            case ScalarSubqueryExpression node:
                VisitScalarSubqueryExpression(node);
                break;
            case ExistsExpression node:
                VisitExistsExpression(node);
                break;
            case UniqueExpression node:
                VisitUniqueExpression(node);
                break;
            case QuantifiedSubqueryExpression node:
                VisitQuantifiedSubqueryExpression(node);
                break;
            case CaseExpression node:
                VisitCaseExpression(node);
                break;
            case StarExpression node:
                VisitStarExpression(node);
                break;
            case QualifiedStarExpression node:
                VisitQualifiedStarExpression(node);
                break;
            case PeriodExpression node:
                VisitPeriodExpression(node);
                break;
            case PeriodPredicateExpression node:
                VisitPeriodPredicateExpression(node);
                break;
            case JsonAccessorExpression node:
                VisitJsonAccessorExpression(node);
                break;
            case MarkupCallExpression node:
                VisitMarkupCallExpression(node);
                break;
            case MdarrayConstructorExpression node:
                VisitMdarrayConstructorExpression(node);
                break;
            case MdarraySliceExpression node:
                VisitMdarraySliceExpression(node);
                break;
            case MdarrayAggregateExpression node:
                VisitMdarrayAggregateExpression(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown expression {expression.GetType()}");
        }
    }

    public virtual void Visit(TableSource table)
    {
        switch (table)
        {
            case TableReference node:
                VisitTableReference(node);
                break;
            case DerivedTable node:
                VisitDerivedTable(node);
                break;
            case JoinedTable node:
                VisitJoinedTable(node);
                break;
            case UnnestTable node:
                VisitUnnestTable(node);
                break;
            case OnlyTable node:
                VisitOnlyTable(node);
                break;
            case MarkupTable node:
                VisitMarkupTable(node);
                break;
            case TableFunction node:
                VisitTableFunction(node);
                break;
            case SampledTable node:
                VisitSampledTable(node);
                break;
            case MatchRecognizeTable node:
                VisitMatchRecognizeTable(node);
                break;
            case PtfTable node:
                VisitPtfTable(node);
                break;
            case GraphTable node:
                VisitGraphTable(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown table source {table.GetType()}");
        }
    }

    public virtual void Visit(JoinConstraint constraint)
    {
        switch (constraint)
        {
            case OnConstraint node:
                VisitOnConstraint(node);
                break;
            case UsingConstraint node:
                VisitUsingConstraint(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown join constraint {constraint.GetType()}");
        }
    }

    public virtual void VisitValuesQuery(ValuesQuery node)
    {
        foreach (var row in node.Rows)
        {
            VisitValuesRow(row);
        }
    }

    public virtual void VisitValuesRow(ValuesRow node) => VisitAll(node.Values);

    public virtual void VisitSetOperation(SetOperation node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitParenQuery(ParenQuery node) => Visit(node.Inner);

    public virtual void VisitWithQuery(WithQuery node)
    {
        foreach (var cte in node.Ctes)
        {
            VisitCommonTableExpression(cte);
        }

        Visit(node.Query);
    }

    public virtual void VisitCommonTableExpression(CommonTableExpression node)
    {
        Visit(node.Query);
        if (node.Cycle is not null)
        {
            VisitCycleClause(node.Cycle);
        }
    }

    public virtual void VisitCycleClause(CycleClause node)
    {
        Visit(node.MarkValue);
        Visit(node.DefaultValue);
    }

    public virtual void VisitInsertStatement(InsertStatement node)
    {
        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitUpdateStatement(UpdateStatement node)
    {
        if (node.Portion is not null)
        {
            VisitPortionClause(node.Portion);
        }

        VisitSetClauses(node.Assignments);
        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }
    }

    public virtual void VisitDeleteStatement(DeleteStatement node)
    {
        if (node.Portion is not null)
        {
            VisitPortionClause(node.Portion);
        }

        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }
    }

    public virtual void VisitMergeStatement(MergeStatement node)
    {
        Visit(node.Source);
        VisitJoins(node.SourceJoins);
        Visit(node.Condition);
        foreach (var when in node.Whens)
        {
            VisitMergeWhenClause(when);
        }
    }

    public virtual void VisitTruncateStatement(TruncateStatement node)
    {
    }

    public virtual void VisitSchemaDefinition(SchemaDefinition node)
    {
        foreach (var element in node.Elements)
        {
            VisitCreateTableStatement(element);
        }
    }

    public virtual void VisitAlterSchemaStatement(AlterSchemaStatement node)
    {
    }

    public virtual void VisitDropSchemaStatement(DropSchemaStatement node)
    {
    }

    public virtual void VisitCreateTableStatement(CreateTableStatement node)
    {
        if (node.Elements is not null)
        {
            foreach (var element in node.Elements)
            {
                Visit(element);
            }
        }

        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void Visit(TableElement element)
    {
        switch (element)
        {
            case ColumnDefinition node:
                VisitColumnDefinition(node);
                break;
            case LikeClause node:
                VisitLikeClause(node);
                break;
            case TableConstraint node:
                VisitTableConstraint(node);
                break;
            case RefIsClause node:
                VisitRefIsClause(node);
                break;
            case PeriodDefinition node:
                VisitPeriodDefinition(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown table element {element.GetType()}");
        }
    }

    public virtual void VisitColumnDefinition(ColumnDefinition node)
    {
        if (node.Type is not null)
        {
            VisitDataType(node.Type);
        }

        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }

        if (node.Identity is not null)
        {
            VisitIdentityColumn(node.Identity);
        }

        if (node.Generated is not null)
        {
            VisitGeneratedColumn(node.Generated);
        }

        foreach (var constraint in node.Constraints)
        {
            VisitTableConstraint(constraint);
        }
    }

    public virtual void VisitLikeClause(LikeClause node)
    {
    }

    public virtual void VisitRefIsClause(RefIsClause node)
    {
    }

    public virtual void VisitPeriodDefinition(PeriodDefinition node)
    {
    }

    public virtual void VisitTableConstraint(TableConstraint node)
    {
        if (node.Check is not null)
        {
            Visit(node.Check);
        }
    }

    public virtual void VisitDefaultClause(DefaultClause node) => Visit(node.Value);

    public virtual void VisitIdentityColumn(IdentityColumn node)
    {
        foreach (var option in node.Options)
        {
            VisitSequenceOption(option);
        }
    }

    public virtual void VisitSequenceOption(SequenceOption node)
    {
        if (node.Value is not null)
        {
            Visit(node.Value);
        }
    }

    public virtual void VisitGeneratedColumn(GeneratedColumn node)
    {
        if (node.Expression is not null)
        {
            Visit(node.Expression);
        }
    }

    public virtual void VisitAlterTableStatement(AlterTableStatement node) => Visit(node.Action);

    public virtual void VisitDropTableStatement(DropTableStatement node)
    {
    }

    public virtual void VisitCreateViewStatement(CreateViewStatement node)
    {
        if (node.Elements is not null)
        {
            foreach (var element in node.Elements)
            {
                Visit(element);
            }
        }

        Visit(node.Query);
    }

    public virtual void VisitAlterViewStatement(AlterViewStatement node) => Visit(node.Action);

    public virtual void Visit(AlterViewAction action)
    {
        switch (action)
        {
            case ReplaceViewAction node:
                VisitReplaceViewAction(node);
                break;
            case AlterViewColumnAction node:
                VisitAlterViewColumnAction(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown alter view action {action.GetType()}");
        }
    }

    public virtual void VisitReplaceViewAction(ReplaceViewAction node) => Visit(node.Query);

    public virtual void VisitAlterViewColumnAction(AlterViewColumnAction node) => Visit(node.Column);

    public virtual void VisitDropViewStatement(DropViewStatement node)
    {
    }

    public virtual void VisitCreateDomainStatement(CreateDomainStatement node)
    {
        VisitDataType(node.Type);
        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }

        foreach (var constraint in node.Constraints)
        {
            VisitDomainConstraint(constraint);
        }
    }

    public virtual void VisitDomainConstraint(DomainConstraint node) => Visit(node.Check);

    public virtual void VisitAlterDomainStatement(AlterDomainStatement node) => Visit(node.Action);

    public virtual void Visit(AlterDomainAction action)
    {
        switch (action)
        {
            case SetDomainDefaultAction node:
                VisitSetDomainDefaultAction(node);
                break;
            case DropDomainDefaultAction node:
                VisitDropDomainDefaultAction(node);
                break;
            case AddDomainConstraintAction node:
                VisitAddDomainConstraintAction(node);
                break;
            case DropDomainConstraintAction node:
                VisitDropDomainConstraintAction(node);
                break;
            default:
                throw new InvalidOperationException($"Unknown alter domain action {action.GetType()}");
        }
    }

    public virtual void VisitSetDomainDefaultAction(SetDomainDefaultAction node) => VisitDefaultClause(node.Default);

    public virtual void VisitDropDomainDefaultAction(DropDomainDefaultAction node)
    {
    }

    public virtual void VisitAddDomainConstraintAction(AddDomainConstraintAction node) => VisitDomainConstraint(node.Constraint);

    public virtual void VisitDropDomainConstraintAction(DropDomainConstraintAction node)
    {
    }

    public virtual void VisitDropDomainStatement(DropDomainStatement node)
    {
    }

    public virtual void VisitCreateTypeStatement(CreateTypeStatement node)
    {
        if (node.Representation is not null)
        {
            VisitDataType(node.Representation);
        }

        if (node.Attributes is not null)
        {
            foreach (var attribute in node.Attributes)
            {
                VisitAttributeDefinition(attribute);
            }
        }

        if (node.Reference?.PredefinedType is not null)
        {
            VisitDataType(node.Reference.PredefinedType);
        }

        foreach (var method in node.Methods)
        {
            VisitMethodSpecification(method);
        }
    }

    public virtual void VisitAttributeDefinition(AttributeDefinition node)
    {
        VisitDataType(node.Type);
        if (node.Default is not null)
        {
            VisitDefaultClause(node.Default);
        }
    }

    public virtual void VisitMethodSpecification(MethodSpecification node)
    {
        foreach (var parameter in node.Parameters)
        {
            VisitRoutineParameter(parameter);
        }

        VisitDataType(node.ReturnsType);
    }

    public virtual void VisitRoutineParameter(RoutineParameter node) => VisitDataType(node.Type);

    public virtual void VisitDropTypeStatement(DropTypeStatement node)
    {
    }

    public virtual void VisitCreateOrderingStatement(CreateOrderingStatement node)
    {
    }

    public virtual void VisitCreateCastStatement(CreateCastStatement node)
    {
        VisitDataType(node.Source);
        VisitDataType(node.Target);
    }

    public virtual void VisitCreateTransformStatement(CreateTransformStatement node)
    {
    }

    public virtual void VisitCreateAssertionStatement(CreateAssertionStatement node) => Visit(node.Check);

    public virtual void VisitDropAssertionStatement(DropAssertionStatement node)
    {
    }

    public virtual void VisitCreateCharacterSetStatement(CreateCharacterSetStatement node)
    {
    }

    public virtual void VisitDropCharacterSetStatement(DropCharacterSetStatement node)
    {
    }

    public virtual void VisitCreateCollationStatement(CreateCollationStatement node)
    {
    }

    public virtual void VisitDropCollationStatement(DropCollationStatement node)
    {
    }

    public virtual void VisitCreateTranslationStatement(CreateTranslationStatement node)
    {
    }

    public virtual void VisitDropTranslationStatement(DropTranslationStatement node)
    {
    }

    public virtual void VisitCreateSequenceStatement(CreateSequenceStatement node)
    {
        if (node.DataType is not null)
        {
            VisitDataType(node.DataType);
        }

        foreach (var option in node.Options)
        {
            VisitSequenceOption(option);
        }
    }

    public virtual void VisitDropSequenceStatement(DropSequenceStatement node)
    {
    }

    public virtual void VisitCreateIndexStatement(CreateIndexStatement node)
    {
        foreach (var column in node.Columns)
        {
            Visit(column.Expression);
        }

        if (node.Where is not null)
        {
            Visit(node.Where);
        }
    }

    public virtual void VisitAlterIndexStatement(AlterIndexStatement node)
    {
    }

    public virtual void VisitDropIndexStatement(DropIndexStatement node)
    {
    }

    public virtual void VisitCommentStatement(CommentStatement node)
    {
    }

    public virtual void VisitGrantPrivilegeStatement(GrantPrivilegeStatement node)
    {
    }

    public virtual void VisitGrantRoleStatement(GrantRoleStatement node)
    {
    }

    public virtual void VisitRevokePrivilegeStatement(RevokePrivilegeStatement node)
    {
    }

    public virtual void VisitRevokeRoleStatement(RevokeRoleStatement node)
    {
    }

    public virtual void VisitCreateRoleStatement(CreateRoleStatement node)
    {
    }

    public virtual void VisitDropRoleStatement(DropRoleStatement node)
    {
    }

    public virtual void VisitSetRoleStatement(SetRoleStatement node)
    {
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
    }

    public virtual void VisitRollbackStatement(RollbackStatement node)
    {
    }

    public virtual void VisitSavepointStatement(SavepointStatement node)
    {
    }

    public virtual void VisitReleaseSavepointStatement(ReleaseSavepointStatement node)
    {
    }

    public virtual void VisitSetConstraintsStatement(SetConstraintsStatement node)
    {
    }

    public virtual void VisitSetSessionAuthorizationStatement(SetSessionAuthorizationStatement node)
    {
    }

    public virtual void VisitSetSessionCharacteristicsStatement(SetSessionCharacteristicsStatement node) =>
        VisitTransactionModes(node.Modes);

    public virtual void VisitSetNamesStatement(SetNamesStatement node)
    {
    }

    public virtual void VisitSetCharacterSetStatement(SetCharacterSetStatement node)
    {
    }

    public virtual void VisitSetCollationStatement(SetCollationStatement node)
    {
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
    }

    public virtual void VisitSetSchemaStatement(SetSchemaStatement node)
    {
    }

    public virtual void VisitSetPathStatement(SetPathStatement node)
    {
    }

    public virtual void VisitConnectStatement(ConnectStatement node)
    {
    }

    public virtual void VisitDisconnectStatement(DisconnectStatement node)
    {
    }

    public virtual void VisitSetConnectionStatement(SetConnectionStatement node)
    {
    }

    public virtual void VisitDeclareCursorStatement(DeclareCursorStatement node) => Visit(node.Query);

    public virtual void VisitOpenStatement(OpenStatement node)
    {
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
    }

    public virtual void VisitAllocateCursorStatement(AllocateCursorStatement node)
    {
    }

    public virtual void VisitDeallocateStatement(DeallocateStatement node)
    {
    }

    public virtual void VisitPrepareStatement(PrepareStatement node)
    {
    }

    public virtual void VisitExecuteStatement(ExecuteStatement node)
    {
    }

    public virtual void VisitExecuteImmediateStatement(ExecuteImmediateStatement node)
    {
    }

    public virtual void VisitDescribeStatement(DescribeStatement node)
    {
    }

    public virtual void VisitDynamicDeclareCursorStatement(DynamicDeclareCursorStatement node)
    {
    }

    public virtual void VisitAllocateDescriptorStatement(AllocateDescriptorStatement node)
    {
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
    }

    public virtual void VisitIterateStatement(IterateStatement node)
    {
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
                throw new InvalidOperationException($"Unknown alter table action {action.GetType()}");
        }
    }

    public virtual void VisitAddColumnAction(AddColumnAction node) => VisitColumnDefinition(node.Column);

    public virtual void VisitDropColumnAction(DropColumnAction node)
    {
    }

    public virtual void VisitAddConstraintAction(AddConstraintAction node) => VisitTableConstraint(node.Constraint);

    public virtual void VisitDropConstraintAction(DropConstraintAction node)
    {
    }

    public virtual void VisitAddPeriodAction(AddPeriodAction node) => VisitPeriodDefinition(node.Period);

    public virtual void VisitDropPeriodAction(DropPeriodAction node)
    {
    }

    public virtual void VisitSystemVersioningAction(SystemVersioningAction node)
    {
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
                throw new InvalidOperationException($"Unknown merge action {action.GetType()}");
        }
    }

    public virtual void VisitMergeUpdateAction(MergeUpdateAction node) => VisitSetClauses(node.Assignments);

    public virtual void VisitMergeDeleteAction(MergeDeleteAction node)
    {
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

    public virtual void VisitSelectStatement(SelectStatement node)
    {
        foreach (var item in node.SelectList)
        {
            VisitSelectItem(item);
        }

        if (node.From is not null)
        {
            Visit(node.From);
        }

        VisitJoins(node.Joins);
        foreach (var extra in node.ExtraFrom)
        {
            VisitCommaFrom(extra);
        }

        if (node.Where is not null)
        {
            VisitWhereClause(node.Where);
        }

        if (node.GroupBy is not null)
        {
            VisitGroupByClause(node.GroupBy);
        }

        if (node.Having is not null)
        {
            VisitHavingClause(node.Having);
        }

        if (node.Window is not null)
        {
            VisitWindowClause(node.Window);
        }

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

    public virtual void VisitSelectItem(SelectItem node) => Visit(node.Expression);

    public virtual void VisitWhereClause(WhereClause node) => Visit(node.Expression);

    public virtual void VisitGroupByClause(GroupByClause node) => VisitAll(node.Keys);

    public virtual void VisitHavingClause(HavingClause node) => Visit(node.Expression);

    public virtual void VisitOrderByClause(OrderByClause node)
    {
        foreach (var item in node.Items)
        {
            VisitOrderByItem(item);
        }
    }

    public virtual void VisitOrderByItem(OrderByItem node) => Visit(node.Expression);

    public virtual void VisitLimitClause(LimitClause node) => Visit(node.Count);

    public virtual void VisitOffsetClause(OffsetClause node) => Visit(node.Count);

    public virtual void VisitFetchClause(FetchClause node)
    {
        if (node.Count is not null)
        {
            Visit(node.Count);
        }
    }

    public virtual void VisitLockClause(LockClause node)
    {
    }

    public virtual void VisitCommaFrom(CommaFrom node)
    {
        Visit(node.Table);
        VisitJoins(node.Joins);
    }

    public virtual void VisitJoinClause(JoinClause node)
    {
        Visit(node.Table);
        if (node.Constraint is not null)
        {
            Visit(node.Constraint);
        }
    }

    public virtual void VisitOnConstraint(OnConstraint node) => Visit(node.Condition);

    public virtual void VisitUsingConstraint(UsingConstraint node)
    {
    }

    public virtual void VisitTableReference(TableReference node)
    {
        if (node.SystemTime is not null)
        {
            VisitSystemTimeClause(node.SystemTime);
        }
    }

    public virtual void VisitDerivedTable(DerivedTable node) => Visit(node.Query);

    public virtual void VisitJoinedTable(JoinedTable node)
    {
        Visit(node.Table);
        VisitJoins(node.Joins);
    }

    public virtual void VisitUnnestTable(UnnestTable node) => VisitAll(node.Expressions);

    public virtual void VisitOnlyTable(OnlyTable node)
    {
        if (node.SystemTime is not null)
        {
            VisitSystemTimeClause(node.SystemTime);
        }
    }

    public virtual void VisitSystemTimeClause(SystemTimeClause node)
    {
        if (node.Point is not null)
        {
            Visit(node.Point);
        }

        if (node.Start is not null)
        {
            Visit(node.Start);
        }

        if (node.End is not null)
        {
            Visit(node.End);
        }
    }

    public virtual void VisitPortionClause(PortionClause node)
    {
        Visit(node.Start);
        Visit(node.End);
    }

    public virtual void VisitPeriodExpression(PeriodExpression node)
    {
        Visit(node.Start);
        Visit(node.End);
    }

    public virtual void VisitPeriodPredicateExpression(PeriodPredicateExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitJsonAccessorExpression(JsonAccessorExpression node)
    {
        Visit(node.Target);
        Visit(node.Index);
    }

    public virtual void VisitMarkupCallExpression(MarkupCallExpression node)
    {
        VisitAll(node.Arguments);
        if (node.Returning is not null)
        {
            VisitDataType(node.Returning);
        }

        if (node.Query is not null)
        {
            Visit(node.Query);
        }

        if (node.OrderBy is not null)
        {
            VisitOrderByClause(node.OrderBy);
        }

        VisitMarkupColumns(node.Columns);
        if (node.Filter is not null)
        {
            VisitFilterClause(node.Filter);
        }

        if (node.Over is not null)
        {
            VisitWindowSpecification(node.Over);
        }
    }

    public virtual void VisitMarkupTable(MarkupTable node)
    {
        VisitAll(node.Arguments);
        VisitMarkupColumns(node.Columns);
    }

    private void VisitMarkupColumns(IReadOnlyList<MarkupColumn> columns)
    {
        foreach (var column in columns)
        {
            if (column.Type is not null)
            {
                VisitDataType(column.Type);
            }

            if (column.Path is not null)
            {
                Visit(column.Path);
            }

            if (column.Default is not null)
            {
                Visit(column.Default);
            }

            VisitMarkupColumns(column.Nested);
        }
    }

    public virtual void VisitTableFunction(TableFunction node)
    {
        VisitAll(node.Arguments);
        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitDirectSqlScript(DirectSqlScript node)
    {
        foreach (var statement in node.Statements)
        {
            Visit(statement);
        }
    }

    public virtual void VisitModuleDefinition(ModuleDefinition node)
    {
        foreach (var content in node.Contents)
        {
            Visit(content);
        }
    }

    public virtual void VisitModuleProcedure(ModuleProcedure node) => Visit(node.Statement);

    public virtual void VisitEmbeddedSqlStatement(EmbeddedSqlStatement node)
    {
        if (node.Statement is not null)
        {
            Visit(node.Statement);
        }
    }

    public virtual void VisitDeclareSectionStatement(DeclareSectionStatement node)
    {
    }

    public virtual void VisitWheneverStatement(WheneverStatement node)
    {
    }

    public virtual void VisitCreatePropertyGraphStatement(CreatePropertyGraphStatement node)
    {
    }

    public virtual void VisitDropPropertyGraphStatement(DropPropertyGraphStatement node)
    {
    }

    public virtual void VisitMdarrayConstructorExpression(MdarrayConstructorExpression node)
    {
        VisitAll(node.Elements);
        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitMdarraySliceExpression(MdarraySliceExpression node)
    {
        Visit(node.Target);
        foreach (var axis in node.Axes)
        {
            if (axis.Lower is not null)
            {
                Visit(axis.Lower);
            }

            if (axis.Upper is not null)
            {
                Visit(axis.Upper);
            }
        }
    }

    public virtual void VisitMdarrayAggregateExpression(MdarrayAggregateExpression node) => Visit(node.Argument);

    public virtual void VisitPtfTable(PtfTable node)
    {
        foreach (var argument in node.Arguments)
        {
            if (argument.Query is not null)
            {
                Visit(argument.Query);
            }

            VisitAll(argument.Values);
            VisitAll(argument.PartitionByList);
            if (argument.OrderBy is not null)
            {
                VisitOrderByClause(argument.OrderBy);
            }

            if (argument.Prune is not null)
            {
                Visit(argument.Prune);
            }

            if (argument.Scalar is not null)
            {
                Visit(argument.Scalar);
            }
        }
    }

    public virtual void VisitGraphTable(GraphTable node)
    {
        if (node.Where is not null)
        {
            Visit(node.Where);
        }

        VisitAll(node.Columns);
        foreach (var element in node.Pattern)
        {
            if (element.Where is not null)
            {
                Visit(element.Where);
            }

            foreach (var property in element.Properties)
            {
                Visit(property.Value);
            }
        }
    }

    public virtual void VisitMatchRecognizeTable(MatchRecognizeTable node)
    {
        Visit(node.Input);
        VisitAll(node.PartitionByList);
        if (node.OrderBy is not null)
        {
            VisitOrderByClause(node.OrderBy);
        }

        foreach (var measure in node.Measures)
        {
            Visit(measure.Expression);
        }

        Visit(node.Pattern);
        foreach (var definition in node.Definitions)
        {
            Visit(definition.Condition);
        }
    }

    public virtual void Visit(RowPattern pattern)
    {
        switch (pattern)
        {
            case PatternPrimary:
                break;
            case PatternConcatenation node:
                foreach (var factor in node.Factors)
                {
                    Visit(factor);
                }

                break;
            case PatternAlternation node:
                foreach (var term in node.Terms)
                {
                    Visit(term);
                }

                break;
            case PatternQuantified node:
                Visit(node.Primary);
                break;
            case PatternGroup node:
                Visit(node.Pattern);
                break;
            case PatternExclusion node:
                Visit(node.Pattern);
                break;
            case PatternPermute node:
                foreach (var item in node.Patterns)
                {
                    Visit(item);
                }

                break;
            default:
                throw new InvalidOperationException($"Unknown row pattern {pattern.GetType()}");
        }
    }

    public virtual void VisitSampledTable(SampledTable node)
    {
        Visit(node.Table);
        Visit(node.Percentage);
        if (node.RepeatArgument is not null)
        {
            Visit(node.RepeatArgument);
        }
    }

    public virtual void VisitCastExpression(CastExpression node)
    {
        Visit(node.Expression);
        VisitDataType(node.Type);
    }

    public virtual void VisitTreatExpression(TreatExpression node)
    {
        Visit(node.Expression);
        VisitDataType(node.Type);
    }

    public virtual void VisitDerefExpression(DerefExpression node) => Visit(node.Expression);

    public virtual void VisitRefValueExpression(RefValueExpression node) => Visit(node.Expression);

    public virtual void VisitDereferenceExpression(DereferenceExpression node) => Visit(node.Reference);

    public virtual void VisitSpecifictypeExpression(SpecifictypeExpression node) => Visit(node.Expression);

    public virtual void VisitMethodInvocationExpression(MethodInvocationExpression node)
    {
        Visit(node.Target);
        if (node.Type is not null)
        {
            VisitDataType(node.Type);
        }

        VisitAll(node.Arguments);
    }

    public virtual void VisitStaticMethodInvocationExpression(StaticMethodInvocationExpression node)
    {
        Visit(node.Type);
        VisitAll(node.Arguments);
    }

    public virtual void VisitNewSpecificationExpression(NewSpecificationExpression node) => VisitAll(node.Arguments);

    public virtual void VisitNullIfExpression(NullIfExpression node)
    {
        Visit(node.First);
        Visit(node.Second);
    }

    public virtual void VisitCoalesceExpression(CoalesceExpression node) => VisitAll(node.Arguments);

    public virtual void VisitNextValueExpression(NextValueExpression node)
    {
    }

    public virtual void VisitColonCastExpression(ColonCastExpression node)
    {
        Visit(node.Expression);
        VisitDataType(node.Type);
    }

    public virtual void VisitDataType(DataType node)
    {
        if (node.Fields is not null)
        {
            foreach (var field in node.Fields)
            {
                VisitDataType(field.Type);
            }
        }

        if (node.ReferencedType is not null)
        {
            VisitDataType(node.ReferencedType);
        }
    }

    public virtual void VisitRowConstructorExpression(RowConstructorExpression node) => VisitAll(node.Elements);

    public virtual void VisitMatchExpression(MatchExpression node)
    {
        Visit(node.Left);
        Visit(node.Query);
    }

    public virtual void VisitOverlapsExpression(OverlapsExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitCollateExpression(CollateExpression node) => Visit(node.Expression);

    public virtual void VisitArrayExpression(ArrayExpression node) => VisitAll(node.Elements);

    public virtual void VisitArrayQueryExpression(ArrayQueryExpression node) => Visit(node.Query);

    public virtual void VisitMultisetExpression(MultisetExpression node) => VisitAll(node.Elements);

    public virtual void VisitMultisetQueryExpression(MultisetQueryExpression node) => Visit(node.Query);

    public virtual void VisitMultisetOperationExpression(MultisetOperationExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitMultisetSetExpression(MultisetSetExpression node) => Visit(node.Expression);

    public virtual void VisitAbsentOnNullExpression(AbsentOnNullExpression node)
    {
    }

    public virtual void VisitIdentifierExpression(IdentifierExpression node)
    {
    }

    public virtual void VisitHostParameterExpression(HostParameterExpression node)
    {
    }

    public virtual void VisitEmbeddedHostExpression(EmbeddedHostExpression node)
    {
    }

    public virtual void VisitLiteralExpression(LiteralExpression node)
    {
    }

    public virtual void VisitNiladicFunctionExpression(NiladicFunctionExpression node)
    {
    }

    public virtual void VisitDatetimeLiteralExpression(DatetimeLiteralExpression node)
    {
    }

    public virtual void VisitIntervalLiteralExpression(IntervalLiteralExpression node) =>
        VisitIntervalQualifier(node.Qualifier);

    public virtual void VisitIntervalQualifier(IntervalQualifier node)
    {
        VisitIntervalField(node.Start);
        if (node.End is not null)
        {
            VisitIntervalField(node.End);
        }
    }

    public virtual void VisitIntervalField(IntervalField node)
    {
    }

    public virtual void VisitTrimExpression(TrimExpression node)
    {
        if (node.Characters is not null)
        {
            Visit(node.Characters);
        }

        Visit(node.Source);
    }

    public virtual void VisitExtractExpression(ExtractExpression node) => Visit(node.Source);

    public virtual void VisitSubstringExpression(SubstringExpression node)
    {
        Visit(node.Source);
        Visit(node.Start);
        if (node.Length is not null)
        {
            Visit(node.Length);
        }
    }

    public virtual void VisitUsingTransformExpression(UsingTransformExpression node) => Visit(node.Expression);

    public virtual void VisitPositionExpression(PositionExpression node)
    {
        Visit(node.Needle);
        Visit(node.Haystack);
    }

    public virtual void VisitSpecialFormExpression(SpecialFormExpression node) => VisitAll(node.Arguments);

    public virtual void VisitGroupingOperationExpression(GroupingOperationExpression node) =>
        VisitAll(node.Elements);

    public virtual void VisitEmptyGroupingSetExpression(EmptyGroupingSetExpression node)
    {
    }

    public virtual void VisitOverlayExpression(OverlayExpression node)
    {
        Visit(node.Source);
        Visit(node.Replacement);
        Visit(node.Start);
        if (node.Length is not null)
        {
            Visit(node.Length);
        }
    }

    public virtual void VisitBinaryExpression(BinaryExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitMemberAccessExpression(MemberAccessExpression node) => Visit(node.Target);

    public virtual void VisitBetweenExpression(BetweenExpression node)
    {
        Visit(node.Target);
        Visit(node.Lower);
        Visit(node.Upper);
    }

    public virtual void VisitInExpression(InExpression node)
    {
        Visit(node.Target);
        VisitAll(node.Values);
        if (node.Query is not null)
        {
            Visit(node.Query);
        }
    }

    public virtual void VisitLikeExpression(LikeExpression node)
    {
        Visit(node.Target);
        Visit(node.Pattern);
        if (node.Escape is not null)
        {
            Visit(node.Escape);
        }
    }

    public virtual void VisitSimilarExpression(SimilarExpression node)
    {
        Visit(node.Target);
        Visit(node.Pattern);
        if (node.Escape is not null)
        {
            Visit(node.Escape);
        }
    }

    public virtual void VisitDistinctFromExpression(DistinctFromExpression node)
    {
        Visit(node.Left);
        Visit(node.Right);
    }

    public virtual void VisitNormalizedPredicateExpression(NormalizedPredicateExpression node) =>
        Visit(node.Target);

    public virtual void VisitIsExpression(IsExpression node) => Visit(node.Target);

    public virtual void VisitUnaryExpression(UnaryExpression node) => Visit(node.Expression);

    public virtual void VisitNotExpression(NotExpression node) => Visit(node.Expression);

    public virtual void VisitFunctionCallExpression(FunctionCallExpression node)
    {
        VisitAll(node.Arguments);
        if (node.Filter is not null)
        {
            VisitFilterClause(node.Filter);
        }

        if (node.Over is not null)
        {
            VisitWindowSpecification(node.Over);
        }
    }

    public virtual void VisitWindowClause(WindowClause node)
    {
        foreach (var window in node.Windows)
        {
            VisitWindowDefinition(window);
        }
    }

    public virtual void VisitWindowDefinition(WindowDefinition node) => VisitWindowSpecification(node.Specification);

    public virtual void VisitWindowSpecification(WindowSpecification node)
    {
        VisitAll(node.PartitionBy);
        if (node.OrderBy is not null)
        {
            VisitOrderByClause(node.OrderBy);
        }

        if (node.Frame is not null)
        {
            VisitWindowFrame(node.Frame);
        }
    }

    public virtual void VisitWindowFrame(WindowFrame node)
    {
        VisitWindowFrameBound(node.Start);
        if (node.End is not null)
        {
            VisitWindowFrameBound(node.End);
        }
    }

    public virtual void VisitWindowFrameBound(WindowFrameBound node)
    {
        if (node.Offset is not null)
        {
            Visit(node.Offset);
        }
    }

    public virtual void VisitFilterClause(FilterClause node) => Visit(node.Expression);

    public virtual void VisitParenExpression(ParenExpression node) => Visit(node.Inner);

    public virtual void VisitScalarSubqueryExpression(ScalarSubqueryExpression node) => Visit(node.Query);

    public virtual void VisitExistsExpression(ExistsExpression node) => Visit(node.Query);

    public virtual void VisitUniqueExpression(UniqueExpression node) => Visit(node.Query);

    public virtual void VisitQuantifiedSubqueryExpression(QuantifiedSubqueryExpression node)
    {
        Visit(node.Left);
        Visit(node.Query);
    }

    public virtual void VisitCaseExpression(CaseExpression node)
    {
        if (node.Operand is not null)
        {
            Visit(node.Operand);
        }

        foreach (var arm in node.Arms)
        {
            VisitWhenClause(arm);
        }

        if (node.ElseResult is not null)
        {
            Visit(node.ElseResult);
        }
    }

    public virtual void VisitWhenClause(WhenClause node)
    {
        Visit(node.Condition);
        Visit(node.Result);
    }

    public virtual void VisitStarExpression(StarExpression node)
    {
    }

    public virtual void VisitQualifiedStarExpression(QualifiedStarExpression node) => Visit(node.Target);

    private void VisitAll(IReadOnlyList<Expression> expressions)
    {
        foreach (var expression in expressions)
        {
            Visit(expression);
        }
    }

    private void VisitJoins(IReadOnlyList<JoinClause> joins)
    {
        foreach (var join in joins)
        {
            VisitJoinClause(join);
        }
    }
}
