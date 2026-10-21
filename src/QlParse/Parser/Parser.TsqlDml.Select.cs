namespace QlParse;

// SQL Server SELECT (workstream C), reached from ParseSelectStatement under SqlOptions.SqlServer:
//   SELECT [ALL | DISTINCT] [TOP ...] select_list [INTO new_table [ON filegroup]] [FROM ...] [WHERE ...]
//   [GROUP BY [ALL] ... [WITH {ROLLUP | CUBE}]] [HAVING ...] [WINDOW ...] [ORDER BY ...]
//   [OFFSET n {ROW | ROWS} [FETCH {FIRST | NEXT} n {ROW | ROWS} ONLY]] [COMPUTE ...] [FOR XML | JSON | BROWSE ...
//   | FOR UPDATE | READ ONLY] [OPTION (...)]
// T-SQL-only parts go into one TsqlSelectExtras, allocated only when at least one is present.
internal sealed partial class Parser
{
    internal SelectStatement ParseTsqlSelectStatement()
    {
        var selectKeyword = Expect(SyntaxKind.SelectKeyword);
        var distinct = _current.Kind == SyntaxKind.DistinctKeyword ? Advance() : (SyntaxToken?)null;
        var all = distinct is null && _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var top = IdentifierEquals(Keyword.DmlTop) ? ParseTsqlTop(isSelect: true) : null;
        var selectList = ParseTsqlSelectList();
        var into = IdentifierEquals(Keyword.Into) ? ParseTsqlSelectInto() : default;
        var fromClause = ParseFromClause();
        var filter = ParseTsqlSelectFilter();
        var rest = ParseTsqlSelectRest();
        var end = rest.End ?? filter.End ?? fromClause.End ?? into.End ?? selectList[^1].Span;
        return new SelectStatement
        {
            Span = SourceSpan.From(selectKeyword, end),
            SelectKeyword = selectKeyword,
            DistinctKeyword = distinct,
            AllKeyword = all,
            SelectList = selectList,
            IntoKeyword = into.IntoKeyword,
            FromKeyword = fromClause.FromKeyword,
            From = fromClause.From,
            Joins = fromClause.Joins,
            ExtraFrom = fromClause.ExtraFrom,
            Where = filter.Where,
            GroupBy = filter.GroupBy,
            Having = filter.Having,
            Window = rest.Window,
            OrderBy = rest.OrderBy,
            Offset = rest.Offset,
            Fetch = rest.Fetch,
            Lock = rest.Lock,
            SqlServer = BuildTsqlSelectExtras(top, into, filter, rest),
        };
    }

    private static TsqlSelectExtras? BuildTsqlSelectExtras(TsqlTopClause? top, TsqlSelectInto into, TsqlSelectFilter filter, TsqlSelectRest rest)
    {
        if (top is null && into.Table is null && !filter.HasSqlServerParts && !rest.HasSqlServerParts)
        {
            return null;
        }

        return new TsqlSelectExtras
        {
            Top = top,
            IntoTable = into.Table,
            IntoOnKeyword = into.OnKeyword,
            IntoFilegroup = into.Filegroup,
            GroupByAllKeyword = filter.All,
            GroupByWithKeyword = filter.With,
            GroupByWithOption = filter.WithOption,
            Compute = rest.Compute,
            For = rest.For,
            Option = rest.Option,
        };
    }

    // ---- Select list ----

    internal List<SelectItem> ParseTsqlSelectList()
    {
        var items = new List<SelectItem> { ParseTsqlSelectItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseTsqlSelectItem());
        }

        return items;
    }

    // expr [[AS] alias], `alias = expr`, `@v = expr` (also `@v += expr`); an alias may be a string literal.
    private SelectItem ParseTsqlSelectItem()
    {
        if (NextKind == SyntaxKind.EqualsToken || NextKind == SyntaxKind.CompoundAssignmentToken)
        {
            var assignment = TryParseTsqlSelectAssignment();
            if (assignment is not null)
            {
                return assignment;
            }
        }

        var expression = _current.Kind == SyntaxKind.Star ? ParseStar() : ParseExpression();
        var alias = ParseTsqlSelectAlias();
        return new SelectItem
        {
            Span = alias.Alias is { } name ? SourceSpan.From(expression.Span, name) : expression.Span,
            Expression = expression,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
        };
    }

    // [AS] alias, where the alias may also be a string literal.
    private TsqlAlias ParseTsqlSelectAlias()
    {
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            var asKeyword = Advance();
            return new TsqlAlias(asKeyword, _current.Kind == SyntaxKind.String ? Advance() : Expect(SyntaxKind.Identifier));
        }

        return _current.Kind == SyntaxKind.String ? new TsqlAlias(null, Advance()) : ParseTsqlAlias();
    }

    private SelectItem? TryParseTsqlSelectAssignment()
    {
        if (_current.Kind == SyntaxKind.EmbeddedHost)
        {
            var variable = Advance();
            var operatorToken = Advance();
            var value = ParseExpression();
            var assignment = new TsqlVariableAssignmentExpression
            {
                Span = SourceSpan.From(variable, value.Span),
                Variable = variable,
                Operator = operatorToken,
                Value = value,
            };
            return new SelectItem { Span = assignment.Span, Expression = assignment };
        }

        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.String) || NextKind != SyntaxKind.EqualsToken)
        {
            return null;
        }

        var alias = Advance();
        var equalsToken = Advance();
        var expression = ParseExpression();
        return new SelectItem
        {
            Span = SourceSpan.From(alias, expression.Span),
            Expression = expression,
            AsKeyword = equalsToken,
            Alias = alias,
        };
    }

    // ---- INTO new_table [ON filegroup] ----

    private readonly record struct TsqlSelectInto(
        SyntaxToken? IntoKeyword,
        SelectIntoTable? Table,
        SyntaxToken? OnKeyword,
        SyntaxToken? Filegroup,
        SourceSpan? End);

    private TsqlSelectInto ParseTsqlSelectInto()
    {
        var intoKeyword = Advance();
        var name = ParseTsqlMultiPartName();
        var table = new SelectIntoTable { Span = SourceSpan.From(name[0], name[^1]), NameParts = name };
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return new TsqlSelectInto(intoKeyword, table, null, null, table.Span);
        }

        var onKeyword = Advance();
        var filegroup = ExpectTsqlDmlWord();
        return new TsqlSelectInto(intoKeyword, table, onKeyword, filegroup, filegroup.Span);
    }

    // ---- WHERE, GROUP BY [ALL] ... [WITH ROLLUP | CUBE], HAVING ----

    private readonly record struct TsqlSelectFilter(
        WhereClause? Where,
        GroupByClause? GroupBy,
        SyntaxToken? All,
        SyntaxToken? With,
        SyntaxToken? WithOption,
        HavingClause? Having,
        SourceSpan? End)
    {
        public bool HasSqlServerParts => All is not null || With is not null;
    }

    private TsqlSelectFilter ParseTsqlSelectFilter()
    {
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        var groupBy = _current.Kind == SyntaxKind.GroupKeyword ? ParseTsqlGroupBy() : default;
        var having = _current.Kind == SyntaxKind.HavingKeyword ? ParseHaving() : null;
        var end = having?.Span ?? groupBy.Clause?.Span ?? where?.Span;
        return new TsqlSelectFilter(where, groupBy.Clause, groupBy.All, groupBy.With, groupBy.WithOption, having, end);
    }

    private readonly record struct TsqlGroupBy(GroupByClause? Clause, SyntaxToken? All, SyntaxToken? With, SyntaxToken? WithOption);

    private TsqlGroupBy ParseTsqlGroupBy()
    {
        var groupKeyword = Advance();
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var all = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var keys = new List<Expression> { ParseGroupingElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            keys.Add(ParseGroupingElement());
        }

        SyntaxToken? with = null;
        SyntaxToken? withOption = null;
        if (_current.Kind == SyntaxKind.WithKeyword && (NextEquals(Keyword.Rollup) || NextEquals(Keyword.Cube)))
        {
            with = Advance();
            withOption = Advance();
        }

        var clause = new GroupByClause
        {
            Span = withOption is null ? SourceSpan.From(groupKeyword, keys[^1].Span) : SourceSpan.From(groupKeyword, withOption.Value),
            GroupKeyword = groupKeyword,
            ByKeyword = byKeyword,
            Keys = keys,
        };
        return new TsqlGroupBy(clause, all, with, withOption);
    }

    // ---- WINDOW, ORDER BY, OFFSET / FETCH, COMPUTE, FOR, OPTION ----

    private readonly record struct TsqlSelectRest(
        WindowClause? Window,
        OrderByClause? OrderBy,
        OffsetClause? Offset,
        FetchClause? Fetch,
        IReadOnlyList<TsqlComputeClause> Compute,
        TsqlForClause? For,
        LockClause? Lock,
        TsqlOptionClause? Option,
        SourceSpan? End)
    {
        public bool HasSqlServerParts => Compute.Count > 0 || For is not null || Option is not null;
    }

    private TsqlSelectRest ParseTsqlSelectRest()
    {
        var ordering = ParseTsqlSelectOrdering();
        var compute = ParseTsqlComputeClauses();
        var forClause = IsTsqlForClause() ? ParseTsqlForClause() : null;
        var lockClause = forClause is null && _current.Kind == SyntaxKind.ForKeyword ? ParseLockClause() : null;
        var option = ParseOptionalTsqlOption();
        var end = option?.Span ?? lockClause?.Span ?? forClause?.Span
            ?? (compute.Count > 0 ? compute[^1].Span : ordering.End);
        return new TsqlSelectRest(ordering.Window, ordering.OrderBy, ordering.Offset, ordering.Fetch, compute, forClause, lockClause, option, end);
    }

    private readonly record struct TsqlSelectOrdering(
        WindowClause? Window,
        OrderByClause? OrderBy,
        OffsetClause? Offset,
        FetchClause? Fetch,
        SourceSpan? End);

    private TsqlSelectOrdering ParseTsqlSelectOrdering()
    {
        var window = _current.Kind == SyntaxKind.WindowKeyword ? ParseWindowClause() : null;
        var orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        var offset = _current.Kind == SyntaxKind.OffsetKeyword ? ParseOffset() : null;

        // FETCH only continues an OFFSET; anywhere else it starts a cursor FETCH statement.
        var fetch = offset is not null && _current.Kind == SyntaxKind.FetchKeyword ? ParseTsqlOffsetFetch() : null;
        var end = fetch?.Span ?? offset?.Span ?? orderBy?.Span ?? window?.Span;
        return new TsqlSelectOrdering(window, orderBy, offset, fetch, end);
    }

    // OFFSET ... FETCH {FIRST | NEXT} n {ROW | ROWS} ONLY: SQL Server has no PERCENT or WITH TIES here.
    private FetchClause ParseTsqlOffsetFetch()
    {
        var fetch = ParseFetch();
        if (fetch.PercentKeyword is { } percent)
        {
            throw new SqlParseException("PERCENT is not valid in OFFSET ... FETCH", percent.Position);
        }

        if (fetch.TiesKeyword is not null)
        {
            throw new SqlParseException("WITH TIES is not valid in OFFSET ... FETCH", fetch.OnlyOrWith.Position);
        }

        return fetch;
    }

    private IReadOnlyList<TsqlComputeClause> ParseTsqlComputeClauses()
    {
        if (!IdentifierEquals(Keyword.DmlCompute))
        {
            return [];
        }

        var clauses = new List<TsqlComputeClause>();
        while (IdentifierEquals(Keyword.DmlCompute))
        {
            var computeKeyword = Advance();
            var functions = ParseExpressionList();
            if (_current.Kind != SyntaxKind.ByKeyword)
            {
                clauses.Add(new TsqlComputeClause { Span = SourceSpan.From(computeKeyword, functions[^1].Span), ComputeKeyword = computeKeyword, Functions = functions });
                continue;
            }

            var byKeyword = Advance();
            var by = ParseExpressionList();
            clauses.Add(new TsqlComputeClause
            {
                Span = SourceSpan.From(computeKeyword, by[^1].Span),
                ComputeKeyword = computeKeyword,
                Functions = functions,
                ByKeyword = byKeyword,
                By = by,
            });
        }

        return clauses;
    }
}
