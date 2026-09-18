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
        return new SelectStatement
        {
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
            return new LockClause
            {
                ForKeyword = forKeyword,
                ReadKeyword = Advance(),
                OnlyKeyword = Expect(SyntaxKind.OnlyKeyword),
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
            ? new StarExpression { Star = Advance() }
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
            Expression = expression,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private GroupByClause ParseGroupBy()
    {
        return new GroupByClause
        {
            GroupKeyword = Expect(SyntaxKind.GroupKeyword),
            ByKeyword = Expect(SyntaxKind.ByKeyword),
            Keys = ParseExpressionList(),
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
            Expression = expression,
            Direction = direction,
        };
    }

    private LimitClause ParseLimit()
    {
        return new LimitClause
        {
            LimitKeyword = Expect(SyntaxKind.LimitKeyword),
            Count = ParseExpression(),
        };
    }

    private OffsetClause ParseOffset()
    {
        return new OffsetClause
        {
            OffsetKeyword = Expect(SyntaxKind.OffsetKeyword),
            Count = ParseExpression(),
        };
    }

    private HavingClause ParseHaving()
    {
        return new HavingClause
        {
            HavingKeyword = Expect(SyntaxKind.HavingKeyword),
            Expression = ParseExpression(),
        };
    }

    private WhereClause ParseWhereClause()
    {
        return new WhereClause
        {
            WhereKeyword = Expect(SyntaxKind.WhereKeyword),
            Expression = ParseExpression(),
        };
    }
}
