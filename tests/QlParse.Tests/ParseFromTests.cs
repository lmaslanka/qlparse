namespace QlParse.Tests;

public sealed class ParseFromTests
{
    [Fact]
    public void Parses_table_name()
    {
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select * from t").From);
        Assert.Single(table.NameParts);
        Assert.Null(table.Alias);
    }

    [Fact]
    public void Parses_dotted_table_name()
    {
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select * from a.b.c").From);
        Assert.Equal(Count.Three, table.NameParts.Count);
    }

    [Fact]
    public void Parses_table_alias_and_columns()
    {
        var withAs = Assert.IsType<TableReference>(SqlAssert.Select("select * from t as x (a, b)").From);
        Assert.NotNull(withAs.AsKeyword);
        Assert.NotNull(withAs.Alias);
        Assert.Equal(Count.Two, withAs.Columns!.Count);

        var bare = Assert.IsType<TableReference>(SqlAssert.Select("select * from t x").From);
        Assert.Null(bare.AsKeyword);
        Assert.NotNull(bare.Alias);
    }

    [Fact]
    public void Parses_quoted_table_name()
    {
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select * from \"from\"").From);
        Assert.Equal(SyntaxKind.Identifier, table.NameParts[0].Kind);
    }

    [Fact]
    public void Parses_unicode_delimited_table_name()
    {
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select * from U&\"from\"").From);
        Assert.Equal(SyntaxKind.Identifier, table.NameParts[0].Kind);
        Assert.Null(table.Alias);
    }

    [Fact]
    public void Parses_character_set_introducer_table_name()
    {
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select * from _latin1\"from\"").From);
        Assert.Single(table.NameParts);
        Assert.Equal(SyntaxKind.Identifier, table.NameParts[0].Kind);
        Assert.Null(table.Alias);
    }

    [Fact]
    public void Parses_derived_table()
    {
        var derived = Assert.IsType<DerivedTable>(SqlAssert.Select("select * from (select 1) as x (a)").From);
        Assert.NotNull(derived.AsKeyword);
        Assert.Single(derived.Columns!);
        Assert.IsType<SelectStatement>(derived.Query);
    }

    [Fact]
    public void Derived_table_requires_alias()
    {
        Assert.NotNull(Sql.Parse("select * from (select 1)").Error);
    }

    [Fact]
    public void Parses_bare_alias_derived_table()
    {
        var derived = Assert.IsType<DerivedTable>(SqlAssert.Select("select * from (select 1) x").From);
        Assert.Null(derived.AsKeyword);
        Assert.Equal(SyntaxKind.Identifier, derived.Alias.Kind);
    }

    [Fact]
    public void Parses_joined_table()
    {
        var joined = Assert.IsType<JoinedTable>(SqlAssert.Select("select * from (t join u on true)").From);
        Assert.Single(joined.Joins);
    }

    [Fact]
    public void Parenthesized_table_without_join_throws()
    {
        Assert.NotNull(Sql.Parse("select * from (t)").Error);
    }

    [Fact]
    public void Parses_comma_from()
    {
        var select = SqlAssert.Select("select * from t, u, v");
        Assert.Equal(Count.Two, select.ExtraFrom.Count);
        Assert.IsType<TableReference>(select.ExtraFrom[0].Table);
    }

    [Fact]
    public void Parses_inner_join_on()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t inner join u on t.a = u.a").Joins);
        Assert.Equal(SyntaxKind.InnerKeyword, join.JoinType!.Value.Kind);
        Assert.IsType<OnConstraint>(join.Constraint);
    }

    [Fact]
    public void Parses_bare_join()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t join u on true").Joins);
        Assert.Null(join.JoinType);
        Assert.IsType<OnConstraint>(join.Constraint);
    }

    [Theory]
    [InlineData("left", SyntaxKind.LeftKeyword, false)]
    [InlineData("left outer", SyntaxKind.LeftKeyword, true)]
    [InlineData("right", SyntaxKind.RightKeyword, false)]
    [InlineData("right outer", SyntaxKind.RightKeyword, true)]
    [InlineData("full", SyntaxKind.FullKeyword, false)]
    [InlineData("full outer", SyntaxKind.FullKeyword, true)]
    public void Parses_outer_joins(string joinType, SyntaxKind kind, bool outer)
    {
        var join = Assert.Single(SqlAssert.Select($"select * from t {joinType} join u on true").Joins);
        Assert.Equal(kind, join.JoinType!.Value.Kind);
        Assert.Equal(outer, join.OuterKeyword is not null);
    }

    [Fact]
    public void Parses_cross_join()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t cross join u").Joins);
        Assert.Equal(SyntaxKind.CrossKeyword, join.JoinType!.Value.Kind);
        Assert.Null(join.Constraint);
    }

    [Fact]
    public void Parses_union_join()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t union join u").Joins);
        Assert.Equal(SyntaxKind.UnionKeyword, join.JoinType!.Value.Kind);
        Assert.Null(join.Constraint);
    }

    [Fact]
    public void Parses_natural_join()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t natural left join u").Joins);
        Assert.NotNull(join.NaturalKeyword);
        Assert.Null(join.Constraint);
    }

    [Fact]
    public void Parses_using()
    {
        var join = Assert.Single(SqlAssert.Select("select * from t join u using (a, b)").Joins);
        var usingConstraint = Assert.IsType<UsingConstraint>(join.Constraint);
        Assert.Equal(Count.Two, usingConstraint.Columns.Count);
    }

    [Fact]
    public void Join_requires_on_or_using()
    {
        Assert.NotNull(Sql.Parse("select * from t join u").Error);
    }

    [Fact]
    public void Outer_without_left_right_full_throws()
    {
        Assert.NotNull(Sql.Parse("select * from t inner outer join u on true").Error);
    }

    [Theory]
    [InlineData("select * from t natural join u on true")]
    [InlineData("select * from t cross join u on true")]
    [InlineData("select * from t union join u on true")]
    [InlineData("select * from t natural join u using (a)")]
    [InlineData("select * from t cross join u using (a)")]
    public void On_and_using_invalid_with_natural_cross_union(string sql)
    {
        Assert.NotNull(Sql.Parse(sql).Error);
    }

    [Fact]
    public void Parses_comma_from_with_joins()
    {
        var select = SqlAssert.Select("select * from t, u join v on true");
        Assert.Single(select.ExtraFrom);
        Assert.Single(select.ExtraFrom[0].Joins);
    }
}
