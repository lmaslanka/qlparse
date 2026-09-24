namespace QlParse.Tests;

public sealed class ParseSelectTests
{
    [Fact]
    public void Parses_select_without_from()
    {
        var select = SqlAssert.Select("select 1");
        Assert.Null(select.FromKeyword);
        Assert.Null(select.From);
        Assert.IsType<LiteralExpression>(select.SelectList[0].Expression);
    }

    [Fact]
    public void Parses_distinct_and_all()
    {
        Assert.NotNull(SqlAssert.Select("select distinct a").DistinctKeyword);
        Assert.NotNull(SqlAssert.Select("select all a").AllKeyword);
        Assert.Null(SqlAssert.Select("select a").DistinctKeyword);
        Assert.Null(SqlAssert.Select("select a").AllKeyword);
    }

    [Fact]
    public void Parses_select_into()
    {
        var select = SqlAssert.Select("select a, b into x, ?, :host from t");
        Assert.NotNull(select.IntoKeyword);
        Assert.Equal(Count.Three, select.IntoTargets!.Count);
        Assert.Equal(SyntaxKind.QuestionMark, select.IntoTargets[1].Kind);
        Assert.Equal(SyntaxKind.EmbeddedHost, select.IntoTargets[Count.Two].Kind);
        Assert.NotNull(select.From);
    }

    [Fact]
    public void Parses_star_and_qualified_star()
    {
        Assert.IsType<StarExpression>(SqlAssert.Select("select *").SelectList[0].Expression);
        Assert.IsType<QualifiedStarExpression>(SqlAssert.Select("select t.*").SelectList[0].Expression);
    }

    [Fact]
    public void Parses_select_list_aliases()
    {
        var items = SqlAssert.Select("select a, b as x, c y").SelectList;
        Assert.Equal(Count.Three, items.Count);
        Assert.Null(items[0].Alias);
        Assert.NotNull(items[1].AsKeyword);
        Assert.NotNull(items[1].Alias);
        Assert.Null(items[Count.Two].AsKeyword);
        Assert.NotNull(items[Count.Two].Alias);
    }

    [Fact]
    public void Exponent_literal_is_not_an_alias()
    {
        var item = SqlAssert.Select("select 1e10").SelectList[0];
        Assert.IsType<LiteralExpression>(item.Expression);
        Assert.Null(item.Alias);
    }

    [Fact]
    public void Number_still_takes_bare_alias()
    {
        var item = SqlAssert.Select("select 1 e").SelectList[0];
        Assert.IsType<LiteralExpression>(item.Expression);
        Assert.NotNull(item.Alias);
    }

    [Fact]
    public void Underscore_identifier_still_takes_bare_alias()
    {
        var item = SqlAssert.Select("select _x y").SelectList[0];
        Assert.IsType<IdentifierExpression>(item.Expression);
        Assert.NotNull(item.Alias);
        Assert.Null(item.AsKeyword);
    }

    [Fact]
    public void Parses_where()
    {
        var select = SqlAssert.Select("select a from t where x = 1");
        Assert.NotNull(select.Where);
        Assert.IsType<BinaryExpression>(select.Where.Expression);
    }

    [Fact]
    public void Parses_group_by_and_having()
    {
        var select = SqlAssert.Select("select a from t group by a, b having count(*) > 1");
        Assert.NotNull(select.GroupBy);
        Assert.Equal(Count.Two, select.GroupBy.Keys.Count);
        Assert.NotNull(select.Having);
    }

    [Fact]
    public void Parses_rollup_cube_and_grouping_sets()
    {
        var select = SqlAssert.Select("select grouping(a, b) from t group by a, rollup (b, c), cube (d), grouping sets ((a, b), rollup (a), ())");
        Assert.Equal(SyntaxKind.GroupingKeyword, Assert.IsType<SpecialFormExpression>(select.SelectList[0].Expression).Name.Kind);
        Assert.Equal(Count.Two, Assert.IsType<SpecialFormExpression>(select.SelectList[0].Expression).Arguments.Count);

        var keys = select.GroupBy!.Keys;
        Assert.IsType<IdentifierExpression>(keys[0]);
        var rollup = Assert.IsType<GroupingOperationExpression>(keys[1]);
        Assert.Null(rollup.SetsKeyword);
        Assert.Equal(Count.Two, rollup.Elements.Count);
        Assert.IsType<GroupingOperationExpression>(keys[Count.Two]);
        var sets = Assert.IsType<GroupingOperationExpression>(keys[Count.Three]);
        Assert.NotNull(sets.SetsKeyword);
        Assert.IsType<RowConstructorExpression>(sets.Elements[0]);
        Assert.IsType<EmptyGroupingSetExpression>(sets.Elements[Count.Two]);
    }

    [Fact]
    public void Parses_order_by()
    {
        var select = SqlAssert.Select("select a from t order by a, b asc, c desc");
        Assert.NotNull(select.OrderBy);
        Assert.Equal(Count.Three, select.OrderBy.Items.Count);
        Assert.Null(select.OrderBy.Items[0].Direction);
        Assert.Equal(SyntaxKind.AscKeyword, select.OrderBy.Items[1].Direction!.Value.Kind);
        Assert.Equal(SyntaxKind.DescKeyword, select.OrderBy.Items[Count.Two].Direction!.Value.Kind);
    }

    [Fact]
    public void Parses_nulls_first_and_last()
    {
        var select = SqlAssert.Select("select a from t order by a nulls first, b desc nulls last");
        Assert.NotNull(select.OrderBy!.Items[0].NullsKeyword);
        Assert.Null(select.OrderBy.Items[0].Direction);
        Assert.Equal(SyntaxKind.DescKeyword, select.OrderBy.Items[1].Direction!.Value.Kind);
        Assert.NotNull(select.OrderBy.Items[1].NullOrder);
    }

    [Fact]
    public void Parses_offset_rows_and_fetch()
    {
        var first = SqlAssert.Select("select a from t offset 5 rows fetch first 10 percent rows with ties");
        Assert.NotNull(first.Offset!.RowKeyword);
        Assert.NotNull(first.Fetch);
        Assert.NotNull(first.Fetch.PercentKeyword);
        Assert.NotNull(first.Fetch.TiesKeyword);
        Assert.Equal(SyntaxKind.WithKeyword, first.Fetch.OnlyOrWith.Kind);

        var next = SqlAssert.Select("select a from t fetch next row only");
        Assert.Null(next.Fetch!.Count);
        Assert.Equal(SyntaxKind.OnlyKeyword, next.Fetch.OnlyOrWith.Kind);
    }

    [Fact]
    public void Parses_limit_and_offset()
    {
        var select = SqlAssert.Select("select a from t limit 1 offset 2");
        Assert.NotNull(select.Limit);
        Assert.NotNull(select.Offset);
    }

    [Fact]
    public void Parses_offset_before_limit()
    {
        var select = SqlAssert.Select("select a from t offset 2 limit 1");
        Assert.NotNull(select.Limit);
        Assert.NotNull(select.Offset);
    }

    [Fact]
    public void Parses_window()
    {
        var inline = SqlAssert.Select("select rank() over (partition by a order by b desc nulls last rows between unbounded preceding and current row exclude ties), dense_rank() over (), percent_rank() over w, cume_dist() over (w), row_number() over (order by a), ntile(4) over (order by a), lag(a, 1, 0) ignore nulls over (order by a), lead(a) respect nulls over (order by a), first_value(a) over (order by a), last_value(a) over (range unbounded preceding), nth_value(a, 2) from last over (groups between 1 preceding and 1 following exclude current row) from t window w as (partition by a), w2 as (w order by b)");
        var rank = Assert.IsType<FunctionCallExpression>(inline.SelectList[0].Expression);
        Assert.NotNull(rank.Over);
        Assert.NotNull(rank.Over.PartitionKeyword);
        Assert.NotNull(rank.Over.OrderBy);
        Assert.NotNull(rank.Over.Frame);
        Assert.NotNull(rank.Over.Frame.ExcludeKeyword);
        Assert.Equal(Count.Two, inline.Window!.Windows.Count);
        Assert.NotNull(inline.Window.Windows[1].Specification.Name);

        var nested = Assert.IsType<FunctionCallExpression>(SqlAssert.Expr("sum(rank() over (order by a)) over (partition by b)"));
        Assert.NotNull(nested.Over);
        Assert.IsType<FunctionCallExpression>(nested.Arguments[0]);
    }

    [Fact]
    public void Parses_for_share()
    {
        var share = SqlAssert.Select("select a from t for share of a, b");
        Assert.NotNull(share.Lock);
        Assert.NotNull(share.Lock.ShareKeyword);
        Assert.Equal(Count.Two, share.Lock.Columns!.Count);
    }

    [Fact]
    public void Parses_for_read_only()
    {
        var select = SqlAssert.Select("select a from t for read only");
        Assert.NotNull(select.Lock);
        Assert.NotNull(select.Lock.ReadKeyword);
        Assert.NotNull(select.Lock.OnlyKeyword);
        Assert.Null(select.Lock.UpdateKeyword);
    }

    [Fact]
    public void Parses_for_update()
    {
        var select = SqlAssert.Select("select a from t for update");
        Assert.NotNull(select.Lock);
        Assert.NotNull(select.Lock.UpdateKeyword);
        Assert.Null(select.Lock.Columns);
    }

    [Fact]
    public void Parses_for_update_of()
    {
        var select = SqlAssert.Select("select a from t for update of a, b");
        Assert.Equal(Count.Two, select.Lock!.Columns!.Count);
    }

    [Fact]
    public void For_without_read_or_update_throws()
    {
        Assert.NotNull(Sql.Parse("select a from t for foo").Error);
    }
}
