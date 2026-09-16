using System.Text;

namespace SqlParser;

internal static class StageDumper
{
    private const string Indent = "  ";
    private const string TableNameLabel = "Name";
    private const int RootChildDepth = 1;
    private const int NestedChildDepth = RootChildDepth + 1;

    public static string DumpTokens(
        string source,
        IReadOnlyList<SyntaxToken> tokens,
        IReadOnlyList<SyntaxTrivia> trivia)
    {
        var dump = new StringBuilder();
        foreach (var token in tokens)
        {
            if (dump.Length > 0)
            {
                dump.AppendLine();
            }

            AppendToken(dump, source, token);
            AppendTrivia(dump, source, trivia, token);
        }

        return dump.ToString();
    }

    public static string DumpTree(string source, Query query)
    {
        if (query is SelectStatement statement)
        {
            return DumpSelect(source, statement);
        }

        var dump = new StringBuilder();
        dump.Append(query.GetType().Name);
        return dump.ToString();
    }

    private static string DumpSelect(string source, SelectStatement statement)
    {
        var dump = new StringBuilder();
        dump.Append(nameof(SelectStatement));
        dump.AppendLine();
        dump.Append(Indent);
        AppendLabeledToken(dump, source, nameof(SelectStatement.SelectKeyword), statement.SelectKeyword);
        if (statement.DistinctKeyword is { } distinct)
        {
            dump.AppendLine();
            dump.Append(Indent);
            AppendLabeledToken(dump, source, nameof(SelectStatement.DistinctKeyword), distinct);
        }

        dump.AppendLine();
        dump.Append(Indent);
        dump.Append(nameof(SelectStatement.SelectList));
        foreach (var item in statement.SelectList)
        {
            dump.AppendLine();
            AppendExpression(dump, source, item.Expression, NestedChildDepth);
        }

        if (statement.FromKeyword is { } fromKeyword && statement.From is { } from)
        {
            dump.AppendLine();
            dump.Append(Indent);
            AppendLabeledToken(dump, source, nameof(SelectStatement.FromKeyword), fromKeyword);
            dump.AppendLine();
            dump.Append(Indent);
            AppendTableSource(dump, source, from, NestedChildDepth);
        }

        foreach (var join in statement.Joins)
        {
            dump.AppendLine();
            dump.Append(Indent);
            dump.Append(nameof(JoinClause));
            dump.AppendLine();
            if (join.NaturalKeyword is { } natural)
            {
                AppendIndent(dump, NestedChildDepth);
                AppendLabeledToken(dump, source, nameof(JoinClause.NaturalKeyword), natural);
                dump.AppendLine();
            }

            if (join.JoinType is { } joinType)
            {
                AppendIndent(dump, NestedChildDepth);
                AppendLabeledToken(dump, source, nameof(JoinClause.JoinType), joinType);
                dump.AppendLine();
            }

            if (join.OuterKeyword is { } outer)
            {
                AppendIndent(dump, NestedChildDepth);
                AppendLabeledToken(dump, source, nameof(JoinClause.OuterKeyword), outer);
                dump.AppendLine();
            }

            AppendIndent(dump, NestedChildDepth);
            AppendLabeledToken(dump, source, nameof(JoinClause.JoinKeyword), join.JoinKeyword);
            if (join.Constraint is OnConstraint on)
            {
                dump.AppendLine();
                AppendExpression(dump, source, on.Condition, NestedChildDepth);
            }
        }

        if (statement.Where is not null)
        {
            dump.AppendLine();
            dump.Append(Indent);
            dump.Append(nameof(WhereClause));
            dump.AppendLine();
            dump.Append(Indent);
            dump.Append(Indent);
            AppendLabeledToken(dump, source, nameof(WhereClause.WhereKeyword), statement.Where.WhereKeyword);
            dump.AppendLine();
            AppendExpression(dump, source, statement.Where.Expression, NestedChildDepth);
        }

        return dump.ToString();
    }

    private static void AppendQueryHead(StringBuilder dump, string source, Query query)
    {
        if (query is SelectStatement select)
        {
            AppendLabeledToken(dump, source, nameof(SelectStatement.SelectKeyword), select.SelectKeyword);
            return;
        }

        dump.Append(query.GetType().Name);
    }

    private static void AppendTableSource(StringBuilder dump, string source, TableSource table, int childDepth)
    {
        switch (table)
        {
            case TableReference named:
                dump.Append(nameof(TableReference));
                dump.AppendLine();
                AppendIndent(dump, childDepth);
                AppendLabeledToken(dump, source, TableNameLabel, named.NameParts[0]);
                if (named.AsKeyword is { } asKeyword)
                {
                    dump.AppendLine();
                    AppendIndent(dump, childDepth);
                    AppendLabeledToken(dump, source, nameof(TableReference.AsKeyword), asKeyword);
                }

                if (named.Alias is { } alias)
                {
                    dump.AppendLine();
                    AppendIndent(dump, childDepth);
                    AppendLabeledToken(dump, source, nameof(TableReference.Alias), alias);
                }

                break;
            case DerivedTable derived:
                dump.Append(nameof(DerivedTable));
                dump.AppendLine();
                AppendIndent(dump, childDepth);
                AppendLabeledToken(dump, source, nameof(DerivedTable.Alias), derived.Alias);
                break;
            default:
                throw new InvalidOperationException($"Unknown table {table.GetType().Name}");
        }
    }

    private static void AppendExpression(StringBuilder dump, string source, Expression expression, int depth)
    {
        AppendIndent(dump, depth);
        switch (expression)
        {
            case ArrayExpression array:
                dump.Append(nameof(ArrayExpression));
                foreach (var element in array.Elements)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, element, depth + 1);
                }

                break;
            case CastExpression cast:
                dump.Append(nameof(CastExpression));
                dump.AppendLine();
                AppendExpression(dump, source, cast.Expression, depth + 1);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, cast.Type.Name);
                break;
            case ColonCastExpression colonCast:
                dump.Append(nameof(ColonCastExpression));
                dump.AppendLine();
                AppendExpression(dump, source, colonCast.Expression, depth + 1);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, colonCast.Type.Name);
                break;
            case IdentifierExpression identifier:
                dump.Append(nameof(IdentifierExpression));
                dump.Append(' ');
                AppendSpan(dump, source, identifier.Identifier);
                break;
            case LiteralExpression literal:
                dump.Append(nameof(LiteralExpression));
                dump.Append(' ');
                AppendSpan(dump, source, literal.Literal);
                break;
            case BinaryExpression binary:
                dump.Append(nameof(BinaryExpression));
                dump.AppendLine();
                AppendExpression(dump, source, binary.Left, depth + 1);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, binary.OperatorToken);
                dump.AppendLine();
                AppendExpression(dump, source, binary.Right, depth + 1);
                break;
            case MemberAccessExpression member:
                dump.Append(nameof(MemberAccessExpression));
                dump.AppendLine();
                AppendExpression(dump, source, member.Target, depth + 1);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, member.Dot);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, member.Member);
                break;
            case BetweenExpression between:
                dump.Append(nameof(BetweenExpression));
                dump.AppendLine();
                AppendExpression(dump, source, between.Target, depth + 1);
                dump.AppendLine();
                AppendExpression(dump, source, between.Lower, depth + 1);
                dump.AppendLine();
                AppendExpression(dump, source, between.Upper, depth + 1);
                break;
            case InExpression inExpression:
                dump.Append(nameof(InExpression));
                dump.AppendLine();
                AppendExpression(dump, source, inExpression.Target, depth + 1);
                if (inExpression.Query is { } inQuery)
                {
                    dump.AppendLine();
                    AppendIndent(dump, depth + 1);
                    AppendQueryHead(dump, source, inQuery);
                    break;
                }

                foreach (var value in inExpression.Values)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, value, depth + 1);
                }

                break;
            case LikeExpression like:
                dump.Append(nameof(LikeExpression));
                dump.AppendLine();
                AppendExpression(dump, source, like.Target, depth + 1);
                dump.AppendLine();
                AppendExpression(dump, source, like.Pattern, depth + 1);
                break;
            case IsNullExpression isNull:
                dump.Append(nameof(IsNullExpression));
                dump.AppendLine();
                AppendExpression(dump, source, isNull.Target, depth + 1);
                break;
            case ParenExpression paren:
                dump.Append(nameof(ParenExpression));
                dump.AppendLine();
                AppendExpression(dump, source, paren.Inner, depth + 1);
                break;
            case QuantifiedSubqueryExpression quantified:
                dump.Append(nameof(QuantifiedSubqueryExpression));
                dump.AppendLine();
                AppendExpression(dump, source, quantified.Left, depth + 1);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendSpan(dump, source, quantified.Quantifier);
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendQueryHead(dump, source, quantified.Query);
                break;
            case ExistsExpression exists:
                dump.Append(nameof(ExistsExpression));
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendQueryHead(dump, source, exists.Query);
                break;
            case ScalarSubqueryExpression subquery:
                dump.Append(nameof(ScalarSubqueryExpression));
                dump.AppendLine();
                AppendIndent(dump, depth + 1);
                AppendQueryHead(dump, source, subquery.Query);
                break;
            case StarExpression star:
                dump.Append(nameof(StarExpression));
                dump.Append(' ');
                AppendSpan(dump, source, star.Star);
                break;
            case QualifiedStarExpression qualifiedStar:
                dump.Append(nameof(QualifiedStarExpression));
                dump.AppendLine();
                AppendExpression(dump, source, qualifiedStar.Target, depth + 1);
                break;
            case UnaryExpression unary:
                dump.Append(nameof(UnaryExpression));
                dump.AppendLine();
                AppendExpression(dump, source, unary.Expression, depth + 1);
                break;
            case NotExpression not:
                dump.Append(nameof(NotExpression));
                dump.AppendLine();
                AppendExpression(dump, source, not.Expression, depth + 1);
                break;
            case FunctionCallExpression call:
                dump.Append(nameof(FunctionCallExpression));
                dump.Append(' ');
                AppendSpan(dump, source, call.Name);
                foreach (var argument in call.Arguments)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, argument, depth + 1);
                }

                break;
            case CaseExpression caseExpression:
                dump.Append(nameof(CaseExpression));
                if (caseExpression.Operand is { } operand)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, operand, depth + 1);
                }

                foreach (var arm in caseExpression.Arms)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, arm.Condition, depth + 1);
                    dump.AppendLine();
                    AppendExpression(dump, source, arm.Result, depth + 1);
                }

                if (caseExpression.ElseResult is { } elseResult)
                {
                    dump.AppendLine();
                    AppendExpression(dump, source, elseResult, depth + 1);
                }

                break;
            default:
                throw new InvalidOperationException($"Unknown expression {expression.GetType().Name}");
        }
    }

    private static void AppendTrivia(
        StringBuilder dump,
        string source,
        IReadOnlyList<SyntaxTrivia> trivia,
        SyntaxToken token)
    {
        var end = token.LeadingTriviaStart + token.LeadingTriviaCount;
        for (var i = token.LeadingTriviaStart; i < end; i++)
        {
            dump.AppendLine();
            dump.Append(Indent);
            var item = trivia[i];
            dump.Append(item.Kind);
            dump.Append(' ');
            AppendQuotedSpan(dump, source, item.Position, item.Length);
            AppendLocation(dump, item.Position, item.Length);
        }
    }

    private static void AppendToken(StringBuilder dump, string source, SyntaxToken token)
    {
        dump.Append(token.Kind);
        dump.Append(' ');
        AppendSpan(dump, source, token);
    }

    private static void AppendLabeledToken(StringBuilder dump, string source, string label, SyntaxToken token)
    {
        dump.Append(label);
        dump.Append(' ');
        AppendSpan(dump, source, token);
    }

    private static void AppendSpan(StringBuilder dump, string source, SyntaxToken token)
    {
        AppendQuotedSpan(dump, source, token.Position, token.Length);
        AppendLocation(dump, token.Position, token.Length);
    }

    private static void AppendQuotedSpan(StringBuilder dump, string source, int position, int length)
    {
        dump.Append('"');
        dump.Append(source.AsSpan(position, length));
        dump.Append('"');
    }

    private static void AppendLocation(StringBuilder dump, int position, int length)
    {
        dump.Append(" @");
        dump.Append(position);
        dump.Append('+');
        dump.Append(length);
    }

    private static void AppendIndent(StringBuilder dump, int depth)
    {
        for (var i = 0; i < depth; i++)
        {
            dump.Append(Indent);
        }
    }
}
