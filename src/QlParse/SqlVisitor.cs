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

    public virtual void VisitCommonTableExpression(CommonTableExpression node) => Visit(node.Query);

    public virtual void VisitInsertStatement(InsertStatement node) => Visit(node.Query);

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
    }

    public virtual void VisitDerivedTable(DerivedTable node) => Visit(node.Query);

    public virtual void VisitJoinedTable(JoinedTable node)
    {
        Visit(node.Table);
        VisitJoins(node.Joins);
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
