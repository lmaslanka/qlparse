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
        Assert.Throws<SqlParseException>(() => Sql.Parse("select a from t for share"));
    }
}
