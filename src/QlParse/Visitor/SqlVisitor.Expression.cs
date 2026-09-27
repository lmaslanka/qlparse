namespace QlParse;

public partial class SqlVisitor
{
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
        return;
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
            foreach (var fieldDef in node.Fields)
            {
                VisitDataType(fieldDef.Type);
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
        return;
    }

    public virtual void VisitIdentifierExpression(IdentifierExpression node)
    {
        return;
    }

    public virtual void VisitHostParameterExpression(HostParameterExpression node)
    {
        return;
    }

    public virtual void VisitEmbeddedHostExpression(EmbeddedHostExpression node)
    {
        return;
    }

    public virtual void VisitLiteralExpression(LiteralExpression node)
    {
        return;
    }

    public virtual void VisitNiladicFunctionExpression(NiladicFunctionExpression node)
    {
        return;
    }

    public virtual void VisitDatetimeLiteralExpression(DatetimeLiteralExpression node)
    {
        return;
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
        return;
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
        return;
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
        return;
    }

    public virtual void VisitQualifiedStarExpression(QualifiedStarExpression node) => Visit(node.Target);

    private void VisitAll(IReadOnlyList<Expression> expressions)
    {
        foreach (var expression in expressions)
        {
            Visit(expression);
        }
    }
}
