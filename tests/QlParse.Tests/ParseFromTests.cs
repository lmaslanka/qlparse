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
    public void Parses_unnest()
    {
        var plain = Assert.IsType<UnnestTable>(SqlAssert.Select("select * from unnest(a)").From);
        Assert.Null(plain.WithKeyword);
        Assert.Null(plain.Alias);

        var ordinal = Assert.IsType<UnnestTable>(SqlAssert.Select("select * from unnest(a, b) with ordinality as x (i, n)").From);
        Assert.NotNull(ordinal.OrdinalityKeyword);
        Assert.NotNull(ordinal.AsKeyword);
        Assert.Equal(Count.Two, ordinal.Expressions.Count);
        Assert.Equal(Count.Two, ordinal.Columns!.Count);
    }

    [Fact]
    public void Parses_lateral()
    {
        var lateral = Assert.IsType<DerivedTable>(SqlAssert.Select("select * from lateral (select 1) as x (a)").From);
        Assert.NotNull(lateral.LateralKeyword);
        Assert.NotNull(lateral.AsKeyword);
        Assert.Single(lateral.Columns!);
    }

    [Fact]
    public void Parses_tablesample()
    {
        var sampled = Assert.IsType<SampledTable>(SqlAssert.Select("select * from t as x tablesample bernoulli (10) repeatable (1)").From);
        Assert.IsType<TableReference>(sampled.Table);
        Assert.NotNull(sampled.RepeatableKeyword);
        Assert.NotNull(sampled.RepeatArgument);

        var system = Assert.IsType<SampledTable>(SqlAssert.Select("select * from t tablesample system (5)").From);
        Assert.Null(system.RepeatableKeyword);
    }

    [Fact]
    public void Parses_only()
    {
        var only = Assert.IsType<OnlyTable>(SqlAssert.Select("select * from only (catalog.schema.person) as p (a, b)").From);
        Assert.Equal(Count.Three, only.NameParts.Count);
        Assert.NotNull(only.AsKeyword);
        Assert.Equal(Count.Two, only.Columns!.Count);
    }

    [Fact]
    public void Parses_table_functions()
    {
        var wrapped = Assert.IsType<TableFunction>(SqlAssert.Select("select * from table(gen(1, 2)) as tf (id)").From);
        Assert.NotNull(wrapped.TableKeyword);
        Assert.Single(wrapped.Arguments);
        Assert.NotNull(wrapped.Alias);

        var routine = Assert.IsType<TableFunction>(SqlAssert.Select("select * from schema.gen(1)").From);
        Assert.Null(routine.TableKeyword);
        Assert.Equal(Count.Two, routine.NameParts.Count);
        Assert.Null(routine.Alias);
    }

    [Fact]
    public void Parses_values_in_from()
    {
        var values = Assert.IsType<DerivedTable>(SqlAssert.Select("select * from (values (1, 2), (3, 4)) as v (a, b)").From);
        Assert.IsType<ValuesQuery>(values.Query);
        Assert.Equal(Count.Two, values.Columns!.Count);
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

    [Fact]
    public void Parses_system_time()
    {
        var asOf = Assert.IsType<TableReference>(SqlAssert.Select("select * from emp for system_time as of date '2011-01-01' as e").From);
        Assert.NotNull(asOf.SystemTime!.AsKeyword);
        Assert.NotNull(asOf.Alias);

        var between = Assert.IsType<TableReference>(SqlAssert.Select("select * from emp for system_time between symmetric date '2011-01-01' and date '2011-06-01'").From);
        Assert.NotNull(between.SystemTime!.BetweenKeyword);
        Assert.NotNull(between.SystemTime.Qualifier);

        var fromTo = Assert.IsType<TableReference>(SqlAssert.Select("select * from emp for system_time from date '2011-01-01' to date '2011-06-01'").From);
        Assert.NotNull(fromTo.SystemTime!.FromKeyword);
        Assert.NotNull(fromTo.SystemTime.ToKeyword);

        var all = Assert.IsType<TableReference>(SqlAssert.Select("select * from emp for system_time all").From);
        Assert.NotNull(all.SystemTime!.AllKeyword);

        var only = Assert.IsType<OnlyTable>(SqlAssert.Select("select * from only (emp) for system_time as of date '2011-01-01'").From);
        Assert.NotNull(only.SystemTime);
    }

    [Fact]
    public void Parses_xml_and_json_tables()
    {
        var xml = Assert.IsType<MarkupTable>(SqlAssert.Select("select * from xmltable ( '/emp' passing x columns id integer path 'id' , n for ordinality ) as e").From);
        Assert.Equal(Count.Two, xml.Columns.Count);
        Assert.NotNull(xml.Alias);
        var json = Assert.IsType<MarkupTable>(SqlAssert.Select("select * from json_table ( j , '$.a' columns ( id integer path '$.id' , nested path '$.phones' columns ( n varchar ( 10 ) path '$.n' ) ) )").From);
        Assert.NotNull(json.Columns[1].Path);
        Assert.Single(json.Columns[1].Nested);
    }

    [Fact]
    public void Parses_match_recognize()
    {
        var matched = Assert.IsType<MatchRecognizeTable>(SqlAssert.Select("""
            select * from t match_recognize (
              partition by a
              order by b
              measures running sum ( a.x ) as s , final count ( b.* ) as c
              all rows per match omit empty matches
              after match skip past last row
              pattern ( ^ a b+ | c* {- d -} )
              subset u = ( a , b )
              define a as a.x > 1 , b as b.y > 2
            ) as mr
            """).From);
        Assert.NotNull(matched.PartitionBy);
        Assert.NotNull(matched.OrderBy);
        Assert.Equal(Count.Two, matched.Measures.Count);
        Assert.Equal(SyntaxKind.AllKeyword, matched.RowsKind!.Value.Kind);
        Assert.NotNull(matched.SkipKeyword);
        Assert.IsType<PatternAlternation>(matched.Pattern);
        Assert.Single(matched.Subsets);
        Assert.Equal(Count.Two, matched.Definitions.Count);
        Assert.NotNull(matched.Alias);

        var one = Assert.IsType<MatchRecognizeTable>(SqlAssert.Select("""
            select * from t match_recognize (
              one row per match
              after match skip to next row
              pattern ( a { 2 , 4 }? )
              define a as a > 0
            )
            """).From);
        Assert.Equal(SyntaxKind.Identifier, one.RowsKind!.Value.Kind);
        Assert.NotNull(one.SkipPosition);
        Assert.IsType<PatternQuantified>(one.Pattern);
    }

    [Fact]
    public void Parses_ptf_and_graph_table()
    {
        var ptf = Assert.IsType<PtfTable>(SqlAssert.Select("""
            select * from my_ptf (
              table ( select a from t ) as src partition by a order by b prune when a > 0 table semantics ,
              row ( 1 , 2 ) row semantics ,
              copartition ( src , other )
            ) as p
            """).From);
        Assert.Equal(Count.Three, ptf.Arguments.Count);
        Assert.NotNull(ptf.Arguments[0].Query);
        Assert.NotNull(ptf.Arguments[0].SemanticsKeyword);
        Assert.NotNull(ptf.Arguments[1].SemanticsKeyword);
        Assert.NotNull(ptf.Arguments[Count.Two].CopartitionKeyword);
        Assert.NotNull(ptf.Alias);

        var graph = Assert.IsType<GraphTable>(SqlAssert.Select("""
            select * from graph_table (
              sch.g match ( a : person { name : 'Ada' } ) -[ e : knows ]-> { 1 , 3 } ( b is person where b.name = 'Bea' ) ,
              ( ( a ) -[ e ]-> ( b ) ) *
              where a.name = 'Ada'
              columns ( a.name as name )
            ) as gt
            """).From);
        Assert.Equal(Count.Two, graph.GraphName.Count);
        Assert.NotNull(graph.Where);
        Assert.NotNull(graph.ColumnsKeyword);
        Assert.NotNull(graph.Alias);
    }
}
