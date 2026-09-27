using System.Runtime.CompilerServices;

namespace QlParse;

public partial class SqlVisitor
{
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

        VisitSelectTail(node);
    }

    private void VisitSelectTail(SelectStatement node)
    {
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
        return;
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
        return;
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
        return;
    }

    public virtual void VisitWheneverStatement(WheneverStatement node)
    {
        return;
    }

    public virtual void VisitCreatePropertyGraphStatement(CreatePropertyGraphStatement node)
    {
        return;
    }

    public virtual void VisitDropPropertyGraphStatement(DropPropertyGraphStatement node)
    {
        return;
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
                throw new SwitchExpressionException(pattern);
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

    private void VisitJoins(IReadOnlyList<JoinClause> joins)
    {
        foreach (var join in joins)
        {
            VisitJoinClause(join);
        }
    }

}
