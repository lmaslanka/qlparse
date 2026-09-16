using System.Text;

namespace SqlParser;

internal sealed class Formatter
{
    private const int AsciiCaseBit = 'a' - 'A';
    private const string Indent = "    ";
    private const string DoubleColon = "::";
    private const string Concat = "||";

    private readonly string _source;
    private readonly IReadOnlyList<SyntaxTrivia> _trivia;
    private readonly StringBuilder _sql;
    private readonly bool[] _emitted;
    private readonly bool _color;
    private int _indent;
    private char _lastChar;

    private Formatter(string source, IReadOnlyList<SyntaxTrivia> trivia, bool color)
    {
        _source = source;
        _trivia = trivia;
        _sql = new StringBuilder(source.Length);
        _emitted = new bool[trivia.Count];
        _color = color;
    }

    public static string Format(
        string source,
        Query statement,
        IReadOnlyList<SyntaxTrivia> trivia,
        SyntaxToken endOfFile,
        bool color = false)
    {
        var formatter = new Formatter(source, trivia, color);
        formatter.Write(statement);
        formatter.AppendLeadingTrivia(endOfFile);
        return formatter._sql.ToString();
    }

    private void Write(Query query)
    {
        switch (query)
        {
            case SelectStatement select:
                WriteSelect(select);
                break;
            case SetOperation setOp:
                WriteSetOperation(setOp);
                break;
            case ParenQuery paren:
                WriteParenQuery(paren);
                break;
            case WithQuery withQuery:
                WriteWithQuery(withQuery);
                break;
            default:
                throw new InvalidOperationException($"Unknown query {query.GetType().Name}");
        }
    }

    private void WriteWithQuery(WithQuery withQuery)
    {
        WriteKeyword(withQuery.WithKeyword, Keyword.WithUpper);
        if (withQuery.RecursiveKeyword is { } recursive)
        {
            AppendPlain(' ');
            WriteKeyword(recursive, Keyword.RecursiveUpper);
        }

        _indent++;
        for (var i = 0; i < withQuery.Ctes.Count; i++)
        {
            AppendLine();
            WriteCte(withQuery.Ctes[i]);
            if (i < withQuery.Ctes.Count - 1)
            {
                AppendPlain(',');
            }
        }

        _indent--;
        AppendLine();
        Write(withQuery.Query);
    }

    private void WriteCte(CommonTableExpression cte)
    {
        WriteIdentifier(cte.Name);
        if (cte.Columns is { } columns)
        {
            AppendPlain('(');
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                {
                    AppendPlain(',');
                    AppendPlain(' ');
                }

                WriteIdentifier(columns[i]);
            }

            AppendPlain(')');
        }

        AppendPlain(' ');
        WriteKeyword(cte.AsKeyword, Keyword.AsUpper);
        AppendPlain(' ');
        AppendSubquery(cte.OpenQuery, cte.Query, cte.CloseQuery);
    }

    private void WriteParenQuery(ParenQuery paren)
    {
        AppendLeadingTrivia(paren.OpenParen);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        AppendPlain('(');
        _indent++;
        AppendLine();
        Write(paren.Inner);
        _indent--;
        AppendLeadingTrivia(paren.CloseParen);
        AppendLine();
        EnsureContentIndent();
        AppendPlain(')');
    }

    private void WriteSetOperation(SetOperation setOp)
    {
        Write(setOp.Left);
        AppendLeadingTrivia(setOp.Operator);
        AppendLine();
        WriteKeyword(setOp.Operator, SetOperatorText(setOp.Operator.Kind));
        if (setOp.AllKeyword is { } all)
        {
            AppendPlain(' ');
            WriteKeyword(all, Keyword.AllUpper);
        }

        AppendLine();
        Write(setOp.Right);
    }

    private void WriteSelect(SelectStatement statement)
    {
        WriteKeyword(statement.SelectKeyword, Keyword.SelectUpper);
        if (statement.DistinctKeyword is { } distinct)
        {
            AppendPlain(' ');
            WriteKeyword(distinct, Keyword.DistinctUpper);
        }

        _indent++;
        for (var i = 0; i < statement.SelectList.Count; i++)
        {
            if (i > 0)
            {
                AppendLeadingTrivia(StartToken(statement.SelectList[i].Expression));
            }

            AppendLine();
            AppendSelectItem(statement.SelectList[i]);
            if (i < statement.SelectList.Count - 1)
            {
                AppendPlain(',');
            }
        }

        _indent--;
        if (statement.FromKeyword is { } fromKeyword && statement.From is { } from)
        {
            AppendLeadingTrivia(fromKeyword);
            AppendLine();
            WriteKeyword(fromKeyword, Keyword.FromUpper);
            AppendPlain(' ');
            AppendTable(from);
            foreach (var join in statement.Joins)
            {
                AppendLine();
                AppendJoin(join);
            }
        }

        if (statement.Where is not null)
        {
            AppendLeadingTrivia(statement.Where.WhereKeyword);
            AppendLine();
            WriteKeyword(statement.Where.WhereKeyword, Keyword.WhereUpper);
            AppendPlain(' ');
            AppendWhereExpression(statement.Where.Expression);
        }

        if (statement.GroupBy is not null)
        {
            AppendLeadingTrivia(statement.GroupBy.GroupKeyword);
            AppendLine();
            WriteKeyword(statement.GroupBy.GroupKeyword, Keyword.GroupUpper);
            AppendPlain(' ');
            WriteKeyword(statement.GroupBy.ByKeyword, Keyword.ByUpper);
            AppendPlain(' ');
            AppendCommaExpressions(statement.GroupBy.Keys);
        }

        if (statement.Having is not null)
        {
            AppendLeadingTrivia(statement.Having.HavingKeyword);
            AppendLine();
            WriteKeyword(statement.Having.HavingKeyword, Keyword.HavingUpper);
            AppendPlain(' ');
            AppendWhereExpression(statement.Having.Expression);
        }

        if (statement.OrderBy is not null)
        {
            AppendLeadingTrivia(statement.OrderBy.OrderKeyword);
            AppendLine();
            WriteKeyword(statement.OrderBy.OrderKeyword, Keyword.OrderUpper);
            AppendPlain(' ');
            WriteKeyword(statement.OrderBy.ByKeyword, Keyword.ByUpper);
            AppendPlain(' ');
            for (var i = 0; i < statement.OrderBy.Items.Count; i++)
            {
                if (i > 0)
                {
                    AppendPlain(',');
                    AppendPlain(' ');
                }

                var item = statement.OrderBy.Items[i];
                AppendExpression(item.Expression);
                if (item.Direction is { } direction)
                {
                    AppendPlain(' ');
                    WriteKeyword(
                        direction,
                        direction.Kind == SyntaxKind.DescKeyword ? Keyword.DescUpper : Keyword.AscUpper);
                }
            }
        }

        if (statement.Limit is not null)
        {
            AppendLeadingTrivia(statement.Limit.LimitKeyword);
            AppendLine();
            WriteKeyword(statement.Limit.LimitKeyword, Keyword.LimitUpper);
            AppendPlain(' ');
            AppendExpression(statement.Limit.Count);
        }

        if (statement.Offset is not null)
        {
            AppendLeadingTrivia(statement.Offset.OffsetKeyword);
            AppendLine();
            WriteKeyword(statement.Offset.OffsetKeyword, Keyword.OffsetUpper);
            AppendPlain(' ');
            AppendExpression(statement.Offset.Count);
        }
    }

    private void AppendSelectItem(SelectItem item)
    {
        AppendExpression(item.Expression);
        if (item.Alias is { } alias)
        {
            AppendPlain(' ');
            if (item.AsKeyword is { } asKeyword)
            {
                WriteKeyword(asKeyword, Keyword.AsUpper);
            }
            else
            {
                AppendColored(Ansi.Keyword, Keyword.AsUpper);
            }

            AppendPlain(' ');
            WriteIdentifier(alias);
        }
    }

    private void AppendCommaExpressions(IReadOnlyList<Expression> expressions)
    {
        for (var i = 0; i < expressions.Count; i++)
        {
            if (i > 0)
            {
                AppendPlain(',');
                AppendPlain(' ');
            }

            AppendExpression(expressions[i]);
        }
    }

    private void AppendJoin(JoinClause join)
    {
        if (join.NaturalKeyword is { } natural)
        {
            WriteKeyword(natural, Keyword.NaturalUpper);
            AppendPlain(' ');
        }

        if (join.JoinType is { } joinType)
        {
            WriteKeyword(joinType, JoinTypeText(joinType.Kind));
            AppendPlain(' ');
        }

        if (join.OuterKeyword is { } outer)
        {
            WriteKeyword(outer, Keyword.OuterUpper);
            AppendPlain(' ');
        }

        WriteKeyword(join.JoinKeyword, Keyword.JoinUpper);
                    AppendPlain(' ');
        AppendTable(join.Table);
        switch (join.Constraint)
        {
            case OnConstraint on:
                _indent++;
                AppendLeadingTrivia(on.OnKeyword);
                AppendLine();
                WriteKeyword(on.OnKeyword, Keyword.OnUpper);
                AppendPlain(' ');
                AppendExpression(on.Condition);
                _indent--;
                break;
            case UsingConstraint usingConstraint:
                _indent++;
                AppendLeadingTrivia(usingConstraint.UsingKeyword);
                AppendLine();
                WriteKeyword(usingConstraint.UsingKeyword, Keyword.UsingUpper);
                AppendPlain(' ');
                AppendPlain('(');
                for (var i = 0; i < usingConstraint.Columns.Count; i++)
                {
                    if (i > 0)
                    {
                        AppendPlain(',');
                        AppendPlain(' ');
                    }

                    WriteIdentifier(usingConstraint.Columns[i]);
                }

                AppendPlain(')');
                _indent--;
                break;
        }
    }

    private static string SetOperatorText(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UnionKeyword => Keyword.UnionUpper,
        SyntaxKind.ExceptKeyword => Keyword.ExceptUpper,
        SyntaxKind.IntersectKeyword => Keyword.IntersectUpper,
        _ => throw new InvalidOperationException($"Unknown set operator {kind}"),
    };

    private static string QuantifierText(SyntaxKind kind) => kind switch
    {
        SyntaxKind.AllKeyword => Keyword.AllUpper,
        SyntaxKind.AnyKeyword => Keyword.AnyUpper,
        SyntaxKind.SomeKeyword => Keyword.SomeUpper,
        _ => throw new InvalidOperationException($"Unknown quantifier {kind}"),
    };

    private static string JoinTypeText(SyntaxKind kind) => kind switch
    {
        SyntaxKind.InnerKeyword => Keyword.InnerUpper,
        SyntaxKind.LeftKeyword => Keyword.LeftUpper,
        SyntaxKind.RightKeyword => Keyword.RightUpper,
        SyntaxKind.FullKeyword => Keyword.FullUpper,
        SyntaxKind.CrossKeyword => Keyword.CrossUpper,
        _ => throw new InvalidOperationException($"Unknown join type {kind}"),
    };

    private void AppendTable(TableSource table)
    {
        switch (table)
        {
            case TableReference named:
                AppendNamedTable(named);
                break;
            case DerivedTable derived:
                AppendDerivedTable(derived);
                break;
            default:
                throw new InvalidOperationException($"Unknown table {table.GetType().Name}");
        }
    }

    private void AppendNamedTable(TableReference table)
    {
        for (var i = 0; i < table.NameParts.Count; i++)
        {
            if (i > 0)
            {
                AppendPlain('.');
            }

            WriteIdentifier(table.NameParts[i]);
        }

        AppendTableAlias(table.AsKeyword, table.Alias);
    }

    private void AppendDerivedTable(DerivedTable table)
    {
        AppendSubquery(table.OpenParen, table.Query, table.CloseParen);
        AppendTableAlias(table.AsKeyword, table.Alias);
    }

    private void AppendSubquery(SyntaxToken openParen, Query query, SyntaxToken closeParen)
    {
        AppendLeadingTrivia(openParen);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        AppendPlain('(');
        _indent++;
        AppendLine();
        Write(query);
        _indent--;
        AppendLeadingTrivia(closeParen);
        AppendLine();
        EnsureContentIndent();
        AppendPlain(')');
    }

    private void AppendTableAlias(SyntaxToken? asKeyword, SyntaxToken? alias)
    {
        if (alias is not { } name)
        {
            return;
        }

        AppendPlain(' ');
        if (asKeyword is { } keyword)
        {
            WriteKeyword(keyword, Keyword.AsUpper);
        }
        else
        {
            AppendColored(Ansi.Keyword, Keyword.AsUpper);
        }

        AppendPlain(' ');
        WriteIdentifier(name);
    }

    private void AppendWhereExpression(Expression expression)
    {
        if (expression is BinaryExpression { OperatorToken.Kind: SyntaxKind.AndKeyword } and)
        {
            AppendWhereExpression(and.Left);
            _indent++;
            AppendLeadingTrivia(and.OperatorToken);
            AppendLine();
            WriteKeyword(and.OperatorToken, Keyword.AndUpper);
            AppendPlain(' ');
            AppendExpression(and.Right);
            _indent--;
            return;
        }

        AppendExpression(expression);
    }

    private void AppendExpression(Expression expression)
    {
        switch (expression)
        {
            case ArrayExpression array:
                WriteKeyword(array.ArrayKeyword, Keyword.ArrayUpper);
                AppendLeadingTrivia(array.OpenBracket);
                AppendPlain('[');
                for (var i = 0; i < array.Elements.Count; i++)
                {
                    if (i > 0)
                    {
                        AppendPlain(',');
                        AppendPlain(' ');
                    }

                    AppendExpression(array.Elements[i]);
                }

                AppendPlain(']');
                break;
            case CastExpression cast:
                WriteKeyword(cast.CastKeyword, Keyword.CastUpper);
                AppendPlain('(');
                AppendExpression(cast.Expression);
                AppendPlain(' ');
                WriteKeyword(cast.AsKeyword, Keyword.AsUpper);
                AppendPlain(' ');
                WriteDataType(cast.Type);
                AppendPlain(')');
                break;
            case ColonCastExpression colonCast:
                AppendExpression(colonCast.Expression);
                AppendLeadingTrivia(colonCast.DoubleColon);
                AppendPlain(DoubleColon);
                WriteDataType(colonCast.Type);
                break;
            case IdentifierExpression identifier:
                WriteIdentifier(identifier.Identifier);
                break;
            case LiteralExpression literal:
                AppendLeadingTrivia(literal.Literal);
                EnsureContentIndent();
                AppendSpaceIfNeeded();
                AppendLiteral(literal.Literal);
                break;
            case BinaryExpression binary:
                AppendExpression(binary.Left);
                AppendPlain(' ');
                AppendOperator(binary.OperatorToken);
                AppendPlain(' ');
                AppendExpression(binary.Right);
                break;
            case MemberAccessExpression member:
                AppendExpression(member.Target);
                AppendPlain('.');
                WriteIdentifier(member.Member);
                break;
            case BetweenExpression between:
                AppendExpression(between.Target);
                AppendPlain(' ');
                WriteKeyword(between.BetweenKeyword, Keyword.BetweenUpper);
                AppendPlain(' ');
                AppendExpression(between.Lower);
                AppendPlain(' ');
                WriteKeyword(between.AndKeyword, Keyword.AndUpper);
                AppendPlain(' ');
                AppendExpression(between.Upper);
                break;
            case InExpression inExpression:
                AppendExpression(inExpression.Target);
                AppendPlain(' ');
                if (inExpression.NotKeyword is { } notIn)
                {
                    WriteKeyword(notIn, Keyword.NotUpper);
                    AppendPlain(' ');
                }

                WriteKeyword(inExpression.InKeyword, Keyword.InUpper);
                AppendPlain(' ');
                if (inExpression.Query is { } inQuery)
                {
                    AppendSubquery(inExpression.OpenParen, inQuery, inExpression.CloseParen);
                    break;
                }

                AppendPlain('(');
                for (var i = 0; i < inExpression.Values.Count; i++)
                {
                    if (i > 0)
                    {
                        AppendPlain(',');
                        AppendPlain(' ');
                    }

                    AppendExpression(inExpression.Values[i]);
                }

                AppendPlain(')');
                break;
            case LikeExpression like:
                AppendExpression(like.Target);
                AppendPlain(' ');
                if (like.NotKeyword is { } notLike)
                {
                    WriteKeyword(notLike, Keyword.NotUpper);
                    AppendPlain(' ');
                }

                WriteKeyword(like.LikeKeyword, Keyword.LikeUpper);
                AppendPlain(' ');
                AppendExpression(like.Pattern);
                break;
            case IsNullExpression isNull:
                AppendExpression(isNull.Target);
                AppendPlain(' ');
                WriteKeyword(isNull.IsKeyword, Keyword.IsUpper);
                AppendPlain(' ');
                if (isNull.NotKeyword is { } notKeyword)
                {
                    WriteKeyword(notKeyword, Keyword.NotUpper);
                    AppendPlain(' ');
                }

                WriteKeyword(isNull.NullKeyword, Keyword.NullUpper);
                break;
            case NotExpression not:
                WriteKeyword(not.NotKeyword, Keyword.NotUpper);
                AppendPlain(' ');
                AppendExpression(not.Expression);
                break;
            case FunctionCallExpression call:
                WriteIdentifier(call.Name);
                AppendPlain('(');
                AppendCommaExpressions(call.Arguments);
                AppendPlain(')');
                break;
            case ParenExpression paren:
                AppendLeadingTrivia(paren.OpenParen);
                EnsureContentIndent();
                AppendPlain('(');
                AppendExpression(paren.Inner);
                AppendPlain(')');
                break;
            case ScalarSubqueryExpression subquery:
                AppendSubquery(subquery.OpenParen, subquery.Query, subquery.CloseParen);
                break;
            case ExistsExpression exists:
                WriteKeyword(exists.ExistsKeyword, Keyword.ExistsUpper);
                AppendPlain(' ');
                AppendSubquery(exists.OpenParen, exists.Query, exists.CloseParen);
                break;
            case QuantifiedSubqueryExpression quantified:
                AppendExpression(quantified.Left);
                AppendPlain(' ');
                AppendOperator(quantified.OperatorToken);
                AppendPlain(' ');
                WriteKeyword(quantified.Quantifier, QuantifierText(quantified.Quantifier.Kind));
                AppendPlain(' ');
                AppendSubquery(quantified.OpenParen, quantified.Query, quantified.CloseParen);
                break;
            case StarExpression star:
                AppendLeadingTrivia(star.Star);
                EnsureContentIndent();
                AppendSpaceIfNeeded();
                AppendPlain('*');
                break;
            case QualifiedStarExpression qualifiedStar:
                AppendExpression(qualifiedStar.Target);
                AppendPlain('.');
                AppendPlain('*');
                break;
            case CaseExpression caseExpression:
                AppendCase(caseExpression);
                break;
            case UnaryExpression unary:
                AppendLeadingTrivia(unary.OperatorToken);
                EnsureContentIndent();
                AppendSpaceIfNeeded();
                AppendPlain(unary.OperatorToken.Kind == SyntaxKind.MinusToken ? '-' : '+');
                AppendExpression(unary.Expression);
                break;
            default:
                throw new InvalidOperationException($"Unknown expression {expression.GetType().Name}");
        }
    }

    private void AppendCase(CaseExpression expression)
    {
        WriteKeyword(expression.CaseKeyword, Keyword.CaseUpper);
        if (expression.Operand is { } operand)
        {
            AppendPlain(' ');
            AppendExpression(operand);
        }

        _indent++;
        foreach (var arm in expression.Arms)
        {
            AppendLeadingTrivia(arm.WhenKeyword);
            AppendLine();
            WriteKeyword(arm.WhenKeyword, Keyword.WhenUpper);
            AppendPlain(' ');
            AppendExpression(arm.Condition);
            AppendPlain(' ');
            WriteKeyword(arm.ThenKeyword, Keyword.ThenUpper);
            AppendPlain(' ');
            AppendExpression(arm.Result);
        }

        if (expression.ElseKeyword is { } elseKeyword && expression.ElseResult is { } elseResult)
        {
            AppendLeadingTrivia(elseKeyword);
            AppendLine();
            WriteKeyword(elseKeyword, Keyword.ElseUpper);
            AppendPlain(' ');
            AppendExpression(elseResult);
        }

        _indent--;
        AppendLeadingTrivia(expression.EndKeyword);
        AppendLine();
        WriteKeyword(expression.EndKeyword, Keyword.EndUpper);
    }

    private void WriteDataType(DataType type)
    {
        WriteTypeName(type.Name);
        if (type.OpenParen is null || type.Precision is not { } precision || type.CloseParen is null)
        {
            return;
        }

        AppendPlain('(');
        AppendPlain(precision.TextOf(_source));
        if (type.Scale is { } scale)
        {
            AppendPlain(',');
            AppendPlain(' ');
            AppendPlain(scale.TextOf(_source));
        }

        AppendPlain(')');
    }

    private void WriteTypeName(SyntaxToken token)
    {
        AppendLeadingTrivia(token);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        if (_color)
        {
            _sql.Append(Ansi.Keyword);
        }

        var span = token.TextOf(_source);
        foreach (var ch in span)
        {
            AppendPlain(ch is >= 'a' and <= 'z' ? (char)(ch - AsciiCaseBit) : ch);
        }

        if (_color)
        {
            _sql.Append(Ansi.Reset);
        }
    }

    private void WriteKeyword(SyntaxToken token, string text)
    {
        AppendLeadingTrivia(token);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        AppendColored(Ansi.Keyword, text);
    }

    private void WriteIdentifier(SyntaxToken token)
    {
        AppendLeadingTrivia(token);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        AppendIdentifier(token);
    }

    private void AppendLeadingTrivia(SyntaxToken token)
    {
        var end = token.LeadingTriviaStart + token.LeadingTriviaCount;
        for (var i = token.LeadingTriviaStart; i < end; i++)
        {
            if (_emitted[i])
            {
                continue;
            }

            AppendComment(_trivia[i]);
            _emitted[i] = true;
        }
    }

    private void AppendComment(SyntaxTrivia comment)
    {
        var text = _source.AsSpan(comment.Position, comment.Length);
        if (comment.Kind == SyntaxKind.LineCommentTrivia)
        {
            if (!AtLineStart())
            {
                AppendSpaceIfNeeded();
            }
            else
            {
                AppendIndent();
            }

            AppendColored(Ansi.Comment, text);
            AppendLine();
            return;
        }

        if (AtLineStart())
        {
            AppendIndent();
            AppendColored(Ansi.Comment, text);
            AppendLine();
            return;
        }

        AppendSpaceIfNeeded();
        AppendColored(Ansi.Comment, text);
    }

    private void AppendSpaceIfNeeded()
    {
        if (_sql.Length == 0)
        {
            return;
        }

        if (_lastChar is ' ' or '\n' or '(' or '[' or '.' or '+' or '-' or ':')
        {
            return;
        }

        AppendPlain(' ');
    }

    private void AppendLine()
    {
        if (!AtLineStart())
        {
            _sql.AppendLine();
            _lastChar = '\n';
        }
    }

    private bool AtLineStart() => _sql.Length == 0 || _lastChar == '\n';

    private void AppendIndent()
    {
        for (var i = 0; i < _indent; i++)
        {
            AppendPlain(Indent);
        }
    }

    private void AppendPlain(char ch)
    {
        _sql.Append(ch);
        _lastChar = ch;
    }

    private void AppendPlain(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        _sql.Append(text);
        _lastChar = text[^1];
    }

    private void AppendPlain(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return;
        }

        _sql.Append(text);
        _lastChar = text[^1];
    }

    private void AppendColored(string color, string text)
    {
        if (_color)
        {
            _sql.Append(color);
        }

        AppendPlain(text);
        if (_color)
        {
            _sql.Append(Ansi.Reset);
        }
    }

    private void AppendColored(string color, ReadOnlySpan<char> text)
    {
        if (_color)
        {
            _sql.Append(color);
        }

        AppendPlain(text);
        if (_color)
        {
            _sql.Append(Ansi.Reset);
        }
    }

    private void AppendLiteral(SyntaxToken literal)
    {
        var color = literal.Kind == SyntaxKind.String ? Ansi.String : Ansi.Number;
        AppendColored(color, literal.TextOf(_source));
    }

    private void EnsureContentIndent()
    {
        if (AtLineStart())
        {
            AppendIndent();
        }
    }

    private static SyntaxToken StartToken(Expression expression) => expression switch
    {
        ArrayExpression array => array.ArrayKeyword,
        CastExpression cast => cast.CastKeyword,
        ColonCastExpression colonCast => StartToken(colonCast.Expression),
        IdentifierExpression identifier => identifier.Identifier,
        LiteralExpression literal => literal.Literal,
        BinaryExpression binary => StartToken(binary.Left),
        MemberAccessExpression member => StartToken(member.Target),
        BetweenExpression between => StartToken(between.Target),
        InExpression inExpression => StartToken(inExpression.Target),
        LikeExpression like => StartToken(like.Target),
        IsNullExpression isNull => StartToken(isNull.Target),
        NotExpression not => not.NotKeyword,
        FunctionCallExpression call => call.Name,
        ParenExpression paren => paren.OpenParen,
        ScalarSubqueryExpression subquery => subquery.OpenParen,
        ExistsExpression exists => exists.ExistsKeyword,
        QuantifiedSubqueryExpression quantified => StartToken(quantified.Left),
        StarExpression star => star.Star,
        QualifiedStarExpression qualifiedStar => StartToken(qualifiedStar.Target),
        CaseExpression caseExpression => caseExpression.CaseKeyword,
        UnaryExpression unary => unary.OperatorToken,
        _ => throw new InvalidOperationException($"Unknown expression {expression.GetType().Name}"),
    };

    private void AppendIdentifier(SyntaxToken token)
    {
        var span = token.TextOf(_source);
        for (var i = 0; i < span.Length; i++)
        {
            var ch = span[i];
            if (ch is >= 'A' and <= 'Z' || !char.IsAscii(ch))
            {
                AppendPlain(span[..i]);
                AppendLowerRest(span[i..]);
                return;
            }
        }

        AppendPlain(span);
    }

    private void AppendLowerRest(ReadOnlySpan<char> span)
    {
        foreach (var ch in span)
        {
            if (ch is >= 'A' and <= 'Z')
            {
                AppendPlain((char)(ch + AsciiCaseBit));
            }
            else if (char.IsAscii(ch))
            {
                AppendPlain(ch);
            }
            else
            {
                AppendPlain(char.ToLowerInvariant(ch));
            }
        }
    }

    private void AppendOperator(SyntaxToken token)
    {
        AppendLeadingTrivia(token);
        EnsureContentIndent();
        AppendSpaceIfNeeded();
        switch (token.Kind)
        {
            case SyntaxKind.ConcatToken:
                AppendPlain(Concat);
                break;
            case SyntaxKind.PlusToken:
                AppendPlain('+');
                break;
            case SyntaxKind.MinusToken:
                AppendPlain('-');
                break;
            case SyntaxKind.Star:
                AppendPlain('*');
                break;
            case SyntaxKind.SlashToken:
                AppendPlain('/');
                break;
            case SyntaxKind.EqualsToken:
                AppendPlain('=');
                break;
            case SyntaxKind.NotEqualsToken:
                AppendPlain('!');
                AppendPlain('=');
                break;
            case SyntaxKind.GreaterThan:
                AppendPlain('>');
                break;
            case SyntaxKind.GreaterOrEqual:
                AppendPlain('>');
                AppendPlain('=');
                break;
            case SyntaxKind.LessThan:
                AppendPlain('<');
                break;
            case SyntaxKind.LessOrEqual:
                AppendPlain('<');
                AppendPlain('=');
                break;
            case SyntaxKind.AndKeyword:
                AppendColored(Ansi.Keyword, Keyword.AndUpper);
                break;
            case SyntaxKind.OrKeyword:
                AppendColored(Ansi.Keyword, Keyword.OrUpper);
                break;
            default:
                throw new InvalidOperationException($"Unknown operator {token.Kind}");
        }
    }
}
