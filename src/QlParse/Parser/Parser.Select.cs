namespace QlParse;

internal sealed partial class Parser
{
    internal SelectStatement ParseSelectStatement()
    {
        var selectKeyword = Expect(SyntaxKind.SelectKeyword);
        var distinct = _current.Kind == SyntaxKind.DistinctKeyword ? Advance() : (SyntaxToken?)null;
        var all = distinct is null && _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var selectList = ParseSelectList();
        var into = ParseIntoClause();
        var fromClause = ParseFromClause();
        var tail = ParseSelectTail();
        var end = tail.End ?? fromClause.End ?? into.End ?? selectList[^1].Span;

        return new SelectStatement
        {
            Span = SourceSpan.From(selectKeyword, end),
            SelectKeyword = selectKeyword,
            DistinctKeyword = distinct,
            AllKeyword = all,
            SelectList = selectList,
            IntoKeyword = into.IntoKeyword,
            IntoTargets = into.IntoTargets,
            FromKeyword = fromClause.FromKeyword,
            From = fromClause.From,
            Joins = fromClause.Joins,
            ExtraFrom = fromClause.ExtraFrom,
            Where = tail.Where,
            GroupBy = tail.GroupBy,
            Having = tail.Having,
            Window = tail.Window,
            OrderBy = tail.OrderBy,
            Limit = tail.Limit,
            Offset = tail.Offset,
            Fetch = tail.Fetch,
            Lock = tail.Lock,
        };
    }

    private readonly record struct IntoClause(SyntaxToken? IntoKeyword, IReadOnlyList<SyntaxToken>? IntoTargets, SourceSpan? End);

    private IntoClause ParseIntoClause()
    {
        if (!IdentifierEquals(Keyword.Into))
        {
            return new IntoClause(null, null, null);
        }

        var intoKeyword = Advance();
        var intoTargets = ParseIntoTargets();
        return new IntoClause(intoKeyword, intoTargets, intoTargets is { Count: > 0 } ? intoTargets[^1].Span : null);
    }

    private readonly record struct FromClause(
        SyntaxToken? FromKeyword,
        TableSource? From,
        IReadOnlyList<JoinClause> Joins,
        IReadOnlyList<CommaFrom> ExtraFrom,
        SourceSpan? End);

    private FromClause ParseFromClause()
    {
        if (_current.Kind != SyntaxKind.FromKeyword)
        {
            return new FromClause(null, null, [], [], null);
        }

        var fromKeyword = Advance();
        var from = ParseTableSource();
        var joins = ParseJoins();
        var extraFrom = ParseCommaFrom();
        var end = (extraFrom.Count > 0 ? extraFrom[^1].Span : (SourceSpan?)null)
            ?? (joins.Count > 0 ? joins[^1].Span : (SourceSpan?)null)
            ?? from.Span;
        return new FromClause(fromKeyword, from, joins, extraFrom, end);
    }

    private readonly record struct SelectTail(
        WhereClause? Where,
        GroupByClause? GroupBy,
        HavingClause? Having,
        WindowClause? Window,
        OrderByClause? OrderBy,
        LimitClause? Limit,
        OffsetClause? Offset,
        FetchClause? Fetch,
        LockClause? Lock,
        SourceSpan? End);

    private SelectTail ParseSelectTail()
    {
        var filter = ParseSelectFilterClauses();
        var rest = ParseSelectRestClauses();
        var end = rest.End ?? filter.End;
        return new SelectTail(
            filter.Where, filter.GroupBy, filter.Having,
            rest.Window, rest.OrderBy, rest.Limit, rest.Offset, rest.Fetch, rest.Lock,
            end);
    }

    private readonly record struct SelectFilterClauses(WhereClause? Where, GroupByClause? GroupBy, HavingClause? Having, SourceSpan? End);

    private SelectFilterClauses ParseSelectFilterClauses()
    {
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        var groupBy = _current.Kind == SyntaxKind.GroupKeyword ? ParseGroupBy() : null;
        var having = _current.Kind == SyntaxKind.HavingKeyword ? ParseHaving() : null;
        var end = having?.Span ?? groupBy?.Span ?? where?.Span;
        return new SelectFilterClauses(where, groupBy, having, end);
    }

    private readonly record struct SelectRestClauses(
        WindowClause? Window,
        OrderByClause? OrderBy,
        LimitClause? Limit,
        OffsetClause? Offset,
        FetchClause? Fetch,
        LockClause? Lock,
        SourceSpan? End);

    private SelectRestClauses ParseSelectRestClauses()
    {
        var window = _current.Kind == SyntaxKind.WindowKeyword ? ParseWindowClause() : null;
        var orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        var limitOffsetFetch = ParseLimitOffsetFetch();
        var limit = limitOffsetFetch.Limit;
        var offset = limitOffsetFetch.Offset;
        var fetch = limitOffsetFetch.Fetch;
        var lockClause = _current.Kind == SyntaxKind.ForKeyword ? ParseLockClause() : null;
        var end = lockClause?.Span
            ?? fetch?.Span
            ?? offset?.Span
            ?? limit?.Span
            ?? orderBy?.Span
            ?? window?.Span;
        return new SelectRestClauses(window, orderBy, limit, offset, fetch, lockClause, end);
    }

    private readonly record struct LimitOffsetFetch(LimitClause? Limit, OffsetClause? Offset, FetchClause? Fetch);

    private LimitOffsetFetch ParseLimitOffsetFetch()
    {
        LimitClause? limit = null;
        OffsetClause? offset = null;
        FetchClause? fetch = null;
        while (true)
        {
            if (limit is null && _current.Kind == SyntaxKind.LimitKeyword)
            {
                limit = ParseLimit();
                continue;
            }

            if (offset is null && _current.Kind == SyntaxKind.OffsetKeyword)
            {
                offset = ParseOffset();
                continue;
            }

            if (fetch is null && _current.Kind == SyntaxKind.FetchKeyword)
            {
                fetch = ParseFetch();
                continue;
            }

            break;
        }

        return new LimitOffsetFetch(limit, offset, fetch);
    }

    private LockClause ParseLockClause()
    {
        var forKeyword = Advance();
        if (_current.Kind == SyntaxKind.ReadKeyword)
        {
            var readKeyword = Advance();
            var onlyKeyword = Expect(SyntaxKind.OnlyKeyword);
            return new LockClause
            {
                Span = SourceSpan.From(forKeyword, onlyKeyword),
                ForKeyword = forKeyword,
                ReadKeyword = readKeyword,
                OnlyKeyword = onlyKeyword,
            };
        }

        if (IdentifierEquals(Keyword.Share))
        {
            var shareKeyword = Advance();
            ParseLockColumns(out var shareOf, out var shareColumns);
            return new LockClause
            {
                Span = SourceSpan.From(forKeyword, shareColumns is null ? shareKeyword : shareColumns[^1]),
                ForKeyword = forKeyword,
                ShareKeyword = shareKeyword,
                OfKeyword = shareOf,
                Columns = shareColumns,
            };
        }

        if (_current.Kind != SyntaxKind.UpdateKeyword)
        {
            throw new SqlParseException("Expected READ, UPDATE, or SHARE", _current.Position);
        }

        var updateKeyword = Advance();
        ParseLockColumns(out var ofKeyword, out var columns);

        return new LockClause
        {
            Span = SourceSpan.From(forKeyword, columns is null ? updateKeyword : columns[^1]),
            ForKeyword = forKeyword,
            UpdateKeyword = updateKeyword,
            OfKeyword = ofKeyword,
            Columns = columns,
        };
    }

    private void ParseLockColumns(out SyntaxToken? ofKeyword, out IReadOnlyList<SyntaxToken>? columns)
    {
        ofKeyword = null;
        columns = null;
        if (_current.Kind != SyntaxKind.OfKeyword)
        {
            return;
        }

        ofKeyword = Advance();
        var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(Expect(SyntaxKind.Identifier));
        }

        columns = names;
    }

    private IReadOnlyList<SelectItem> ParseSelectList()
    {
        var items = new List<SelectItem> { ParseSelectItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseSelectItem());
        }

        return items;
    }

    private SelectItem ParseSelectItem()
    {
        var expression = _current.Kind == SyntaxKind.Star
            ? ParseStar()
            : ParseExpression();
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else
        {
            if (_current.Kind == SyntaxKind.Identifier && !IdentifierEquals(Keyword.Into))
            {
                alias = Advance();
            }
        }

        return new SelectItem
        {
            Span = alias is null ? expression.Span : SourceSpan.From(expression.Span, alias.Value),
            Expression = expression,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private IReadOnlyList<SyntaxToken> ParseIntoTargets()
    {
        var targets = new List<SyntaxToken> { ParseIntoTarget() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            targets.Add(ParseIntoTarget());
        }

        return targets;
    }

    private SyntaxToken ParseIntoTarget()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected INTO target, found {_current.Kind}", _current.Position);
    }

    private GroupByClause ParseGroupBy()
    {
        var groupKeyword = Expect(SyntaxKind.GroupKeyword);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var keys = new List<Expression> { ParseGroupingElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            keys.Add(ParseGroupingElement());
        }

        return new GroupByClause
        {
            Span = SourceSpan.From(groupKeyword, keys[^1].Span),
            GroupKeyword = groupKeyword,
            ByKeyword = byKeyword,
            Keys = keys,
        };
    }

    private Expression ParseGroupingElement()
    {
        if (IsGroupingOperation())
        {
            return ParseGroupingOperation();
        }

        if (_current.Kind == SyntaxKind.OpenParen && NextKind == SyntaxKind.CloseParen)
        {
            var openParen = Advance();
            var closeParen = Advance();
            return new EmptyGroupingSetExpression
            {
                Span = SourceSpan.From(openParen, closeParen),
                OpenParen = openParen,
                CloseParen = closeParen,
            };
        }

        return ParseExpression();
    }

    private bool IsGroupingOperation() =>
        (IdentifierEquals(Keyword.Rollup) || IdentifierEquals(Keyword.Cube)) && NextKind == SyntaxKind.OpenParen
        || _current.Kind == SyntaxKind.GroupingKeyword && NextIsSets();

    private bool NextIsSets() =>
        NextKind == SyntaxKind.Identifier
        && _index < _tokens.Count
        && TokenEquals(_tokens[_index], Keyword.Sets);

    private GroupingOperationExpression ParseGroupingOperation()
    {
        var keyword = Advance();
        SyntaxToken? setsKeyword = keyword.Kind == SyntaxKind.GroupingKeyword ? Advance() : null;
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = new List<Expression> { ParseGroupingElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseGroupingElement());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new GroupingOperationExpression
        {
            Span = SourceSpan.From(keyword, closeParen),
            Keyword = keyword,
            SetsKeyword = setsKeyword,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
        };
    }

    private OrderByClause ParseOrderBy()
    {
        var orderKeyword = Expect(SyntaxKind.OrderKeyword);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var items = new List<OrderByItem> { ParseOrderByItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseOrderByItem());
        }

        return new OrderByClause
        {
            Span = SourceSpan.From(orderKeyword, items[^1].Span),
            OrderKeyword = orderKeyword,
            ByKeyword = byKeyword,
            Items = items,
        };
    }

    private OrderByItem ParseOrderByItem()
    {
        var expression = ParseExpression();
        SyntaxToken? direction = null;
        if (_current.Kind is SyntaxKind.AscKeyword or SyntaxKind.DescKeyword)
        {
            direction = Advance();
        }

        SyntaxToken? nullsKeyword = null;
        SyntaxToken? nullOrder = null;
        if (IdentifierEquals(Keyword.Nulls))
        {
            nullsKeyword = Advance();
            if (!IdentifierEquals(Keyword.First) && !IdentifierEquals(Keyword.Last))
            {
                throw new SqlParseException($"Expected FIRST or LAST, found {_current.Kind}", _current.Position);
            }

            nullOrder = Advance();
        }

        var end = nullOrder ?? direction ?? (SyntaxToken?)null;
        return new OrderByItem
        {
            Span = end is null ? expression.Span : SourceSpan.From(expression.Span, end.Value),
            Expression = expression,
            Direction = direction,
            NullsKeyword = nullsKeyword,
            NullOrder = nullOrder,
        };
    }

    private LimitClause ParseLimit()
    {
        var limitKeyword = Expect(SyntaxKind.LimitKeyword);
        var count = ParseExpression();
        return new LimitClause
        {
            Span = SourceSpan.From(limitKeyword, count.Span),
            LimitKeyword = limitKeyword,
            Count = count,
        };
    }

    private OffsetClause ParseOffset()
    {
        var offsetKeyword = Expect(SyntaxKind.OffsetKeyword);
        var count = ParseExpression();
        SyntaxToken? rowKeyword = null;
        if (IsRowKeyword())
        {
            rowKeyword = Advance();
        }

        var end = rowKeyword ?? (SyntaxToken?)null;
        return new OffsetClause
        {
            Span = end is null ? SourceSpan.From(offsetKeyword, count.Span) : SourceSpan.From(offsetKeyword, end.Value),
            OffsetKeyword = offsetKeyword,
            Count = count,
            RowKeyword = rowKeyword,
        };
    }

    private FetchClause ParseFetch()
    {
        var fetchKeyword = Expect(SyntaxKind.FetchKeyword);
        if (!IdentifierEquals(Keyword.First) && !IdentifierEquals(Keyword.Next))
        {
            throw new SqlParseException($"Expected FIRST or NEXT, found {_current.Kind}", _current.Position);
        }

        var positionKeyword = Advance();
        Expression? count = null;
        SyntaxToken? percentKeyword = null;
        if (!IsRowKeyword())
        {
            count = ParseExpression();
            if (IdentifierEquals(Keyword.Percent))
            {
                percentKeyword = Advance();
            }
        }

        if (IsRowKeyword() is false)
        {
            throw new SqlParseException($"Expected ROW or ROWS, found {_current.Kind}", _current.Position);
        }

        var rowKeyword = Advance();
        SyntaxToken onlyOrWith;
        SyntaxToken? tiesKeyword = null;
        if (_current.Kind == SyntaxKind.OnlyKeyword)
        {
            onlyOrWith = Advance();
        }
        else
        {
            if (_current.Kind == SyntaxKind.WithKeyword)
            {
                onlyOrWith = Advance();
                if (!IdentifierEquals(Keyword.Ties))
                {
                    throw new SqlParseException($"Expected TIES, found {_current.Kind}", _current.Position);
                }

                tiesKeyword = Advance();
            }
            else
            {
                throw new SqlParseException($"Expected ONLY or WITH TIES, found {_current.Kind}", _current.Position);
            }
        }

        var end = tiesKeyword ?? onlyOrWith;
        return new FetchClause
        {
            Span = SourceSpan.From(fetchKeyword, end),
            FetchKeyword = fetchKeyword,
            PositionKeyword = positionKeyword,
            Count = count,
            PercentKeyword = percentKeyword,
            RowKeyword = rowKeyword,
            OnlyOrWith = onlyOrWith,
            TiesKeyword = tiesKeyword,
        };
    }

    private bool IsRowKeyword() =>
        IdentifierEquals(Keyword.Row) || IdentifierEquals(Keyword.Rows);

    private HavingClause ParseHaving()
    {
        var havingKeyword = Expect(SyntaxKind.HavingKeyword);
        var expression = ParseExpression();
        return new HavingClause
        {
            Span = SourceSpan.From(havingKeyword, expression.Span),
            HavingKeyword = havingKeyword,
            Expression = expression,
        };
    }

    private WhereClause ParseWhereClause()
    {
        var whereKeyword = Expect(SyntaxKind.WhereKeyword);
        var expression = ParseExpression();
        return new WhereClause
        {
            Span = SourceSpan.From(whereKeyword, expression.Span),
            WhereKeyword = whereKeyword,
            Expression = expression,
        };
    }
}
