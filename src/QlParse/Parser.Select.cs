namespace QlParse;

internal sealed partial class Parser
{
    private SelectStatement ParseSelectStatement()
    {
        var selectKeyword = Expect(SyntaxKind.SelectKeyword);
        var distinct = _current.Kind == SyntaxKind.DistinctKeyword ? Advance() : (SyntaxToken?)null;
        var all = distinct is null && _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var selectList = ParseSelectList();
        SyntaxToken? fromKeyword = null;
        TableSource? from = null;
        IReadOnlyList<JoinClause> joins = [];
        IReadOnlyList<CommaFrom> extraFrom = [];
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            from = ParseTableSource();
            joins = ParseJoins();
            extraFrom = ParseCommaFrom();
        }
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        var groupBy = _current.Kind == SyntaxKind.GroupKeyword ? ParseGroupBy() : null;
        var having = _current.Kind == SyntaxKind.HavingKeyword ? ParseHaving() : null;
        var orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        LimitClause? limit = null;
        OffsetClause? offset = null;
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

            break;
        }

        var lockClause = _current.Kind == SyntaxKind.ForKeyword ? ParseLockClause() : null;
        var end = selectList[^1].Span;
        if (from is not null)
        {
            end = from.Span;
        }

        if (joins.Count > 0)
        {
            end = joins[^1].Span;
        }

        if (extraFrom.Count > 0)
        {
            end = extraFrom[^1].Span;
        }

        if (where is not null)
        {
            end = where.Span;
        }

        if (groupBy is not null)
        {
            end = groupBy.Span;
        }

        if (having is not null)
        {
            end = having.Span;
        }

        if (orderBy is not null)
        {
            end = orderBy.Span;
        }

        if (limit is not null)
        {
            end = limit.Span;
        }

        if (offset is not null)
        {
            end = offset.Span;
        }

        if (lockClause is not null)
        {
            end = lockClause.Span;
        }

        return new SelectStatement
        {
            Span = SourceSpan.From(selectKeyword, end),
            SelectKeyword = selectKeyword,
            DistinctKeyword = distinct,
            AllKeyword = all,
            SelectList = selectList,
            FromKeyword = fromKeyword,
            From = from,
            Joins = joins,
            ExtraFrom = extraFrom,
            Where = where,
            GroupBy = groupBy,
            Having = having,
            OrderBy = orderBy,
            Limit = limit,
            Offset = offset,
            Lock = lockClause,
        };
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

        if (_current.Kind != SyntaxKind.UpdateKeyword)
        {
            throw new SqlParseException("Expected READ or UPDATE", _current.Position);
        }

        var updateKeyword = Advance();
        SyntaxToken? ofKeyword = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        if (_current.Kind == SyntaxKind.OfKeyword)
        {
            ofKeyword = Advance();
            var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(Expect(SyntaxKind.Identifier));
            }

            columns = names;
        }

        return new LockClause
        {
            Span = SourceSpan.From(forKeyword, columns is null ? updateKeyword : columns[^1]),
            ForKeyword = forKeyword,
            UpdateKeyword = updateKeyword,
            OfKeyword = ofKeyword,
            Columns = columns,
        };
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
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }

        return new SelectItem
        {
            Span = alias is null ? expression.Span : SourceSpan.From(expression.Span, alias.Value),
            Expression = expression,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private GroupByClause ParseGroupBy()
    {
        var groupKeyword = Expect(SyntaxKind.GroupKeyword);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var keys = ParseExpressionList();
        return new GroupByClause
        {
            Span = SourceSpan.From(groupKeyword, keys[^1].Span),
            GroupKeyword = groupKeyword,
            ByKeyword = byKeyword,
            Keys = keys,
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

        return new OrderByItem
        {
            Span = direction is null ? expression.Span : SourceSpan.From(expression.Span, direction.Value),
            Expression = expression,
            Direction = direction,
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
        return new OffsetClause
        {
            Span = SourceSpan.From(offsetKeyword, count.Span),
            OffsetKeyword = offsetKeyword,
            Count = count,
        };
    }

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
