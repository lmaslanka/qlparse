using System.Runtime.CompilerServices;

namespace QlParse;

public partial class SqlVisitor
{
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
                throw new SwitchExpressionException(table);
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
                throw new SwitchExpressionException(constraint);
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
        return;
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
        return;
    }

    public virtual void VisitDropSchemaStatement(DropSchemaStatement node)
    {
        return;
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
                throw new SwitchExpressionException(element);
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
        return;
    }

    public virtual void VisitRefIsClause(RefIsClause node)
    {
        return;
    }

    public virtual void VisitPeriodDefinition(PeriodDefinition node)
    {
        return;
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
        return;
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
                throw new SwitchExpressionException(action);
        }
    }

    public virtual void VisitReplaceViewAction(ReplaceViewAction node) => Visit(node.Query);

    public virtual void VisitAlterViewColumnAction(AlterViewColumnAction node) => Visit(node.Column);

    public virtual void VisitDropViewStatement(DropViewStatement node)
    {
        return;
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
                throw new SwitchExpressionException(action);
        }
    }

    public virtual void VisitSetDomainDefaultAction(SetDomainDefaultAction node) => VisitDefaultClause(node.Default);

    public virtual void VisitDropDomainDefaultAction(DropDomainDefaultAction node)
    {
        return;
    }

    public virtual void VisitAddDomainConstraintAction(AddDomainConstraintAction node) => VisitDomainConstraint(node.Constraint);

    public virtual void VisitDropDomainConstraintAction(DropDomainConstraintAction node)
    {
        return;
    }

    public virtual void VisitDropDomainStatement(DropDomainStatement node)
    {
        return;
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
        return;
    }

    public virtual void VisitCreateOrderingStatement(CreateOrderingStatement node)
    {
        return;
    }

    public virtual void VisitCreateCastStatement(CreateCastStatement node)
    {
        VisitDataType(node.Source);
        VisitDataType(node.Target);
    }

    public virtual void VisitCreateTransformStatement(CreateTransformStatement node)
    {
        return;
    }

    public virtual void VisitCreateAssertionStatement(CreateAssertionStatement node) => Visit(node.Check);

    public virtual void VisitDropAssertionStatement(DropAssertionStatement node)
    {
        return;
    }

    public virtual void VisitCreateCharacterSetStatement(CreateCharacterSetStatement node)
    {
        return;
    }

    public virtual void VisitDropCharacterSetStatement(DropCharacterSetStatement node)
    {
        return;
    }

    public virtual void VisitCreateCollationStatement(CreateCollationStatement node)
    {
        return;
    }

    public virtual void VisitDropCollationStatement(DropCollationStatement node)
    {
        return;
    }

    public virtual void VisitCreateTranslationStatement(CreateTranslationStatement node)
    {
        return;
    }

    public virtual void VisitDropTranslationStatement(DropTranslationStatement node)
    {
        return;
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
        return;
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
        return;
    }

    public virtual void VisitDropIndexStatement(DropIndexStatement node)
    {
        return;
    }

}
