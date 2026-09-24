namespace QlParse.Tests;

public sealed class VisitorTests
{
    private sealed class EmptyVisitor : SqlVisitor;

    private sealed class TypeRecorder : SqlVisitor
    {
        public List<Type> Types { get; } = [];

        public override void VisitSelectStatement(SelectStatement node)
        {
            Types.Add(typeof(SelectStatement));
            base.VisitSelectStatement(node);
        }

        public override void VisitIdentifierExpression(IdentifierExpression node)
        {
            Types.Add(typeof(IdentifierExpression));
            base.VisitIdentifierExpression(node);
        }

        public override void VisitTableReference(TableReference node)
        {
            Types.Add(typeof(TableReference));
            base.VisitTableReference(node);
        }
    }

    [Fact]
    public void Default_visitor_walks_select()
    {
        new EmptyVisitor().Visit(SqlAssert.Select("select a from t"));
    }

    [Fact]
    public void Records_select_identifier_and_table()
    {
        var visitor = new TypeRecorder();
        visitor.Visit(SqlAssert.Select("select a from t"));
        Assert.Contains(typeof(SelectStatement), visitor.Types);
        Assert.Contains(typeof(IdentifierExpression), visitor.Types);
        Assert.Contains(typeof(TableReference), visitor.Types);
    }

    [Fact]
    public void Walks_subquery_identifiers()
    {
        var visitor = new TypeRecorder();
        visitor.Visit(SqlAssert.Select("select (select b from u)"));
        Assert.Contains(typeof(IdentifierExpression), visitor.Types);
        Assert.Contains(typeof(TableReference), visitor.Types);
    }

    [Fact]
    public void Default_visitor_walks_dense_query()
    {
        var visitor = new EmptyVisitor();
        visitor.Visit(ParseQuery("with cte as (select 1) search depth first by id set seq cycle id set mark to 1 default 0 using path select distinct a, t.*, count(*) filter (where x > 1), cast(y as int), y::text, array[1], case when z then 1 else 0 end, exists (select 1), trim(both from n) from t as x (c) join (select 1) as d on true left join u using (id), v where a between 1 and 2 and a in (select 1) and a like 'x' and a is null group by a having count(*) > 1 order by a desc limit 1 offset 0 for update"));
        visitor.Visit(ParseQuery("values (1), (2)"));
        visitor.Visit(ParseQuery("select 1 union all corresponding by (a) select 2"));
        visitor.Visit(SqlAssert.Expr("interval '1' day to hour"));
        visitor.Visit(SqlAssert.Expr("date '2020-01-01'"));
        visitor.Visit(SqlAssert.Expr("not x"));
        visitor.Visit(SqlAssert.Expr("- x"));
        visitor.Visit(SqlAssert.Expr("(1, 2) overlaps (3, 4)"));
        visitor.Visit(SqlAssert.Expr("a match unique partial (select 1)"));
        visitor.Visit(SqlAssert.Expr("unique (select 1)"));
        visitor.Visit(SqlAssert.Expr("a = all (select 1)"));
        visitor.Visit(SqlAssert.Expr("a similar to 'x' escape '/'"));
        visitor.Visit(SqlAssert.Expr("a is not distinct from b"));
        visitor.Visit(SqlAssert.Expr("a is nfc normalized"));
        visitor.Visit(SqlAssert.Expr("convert(x using utf8)"));
        visitor.Visit(SqlAssert.Expr("substring(x from 1 for 2)"));
        visitor.Visit(SqlAssert.Expr("extract(year from x)"));
        visitor.Visit(SqlAssert.Expr("position('a' in x)"));
        visitor.Visit(SqlAssert.Expr("a collate en_us"));
        visitor.Visit(SqlAssert.Select("select * from (t join u on true)"));
        visitor.Visit(SqlAssert.Expr("array(select 1)"));
        visitor.Visit(SqlAssert.Expr("multiset[1] multiset union all multiset(select 1)"));
        visitor.Visit(SqlAssert.Expr("table(select 1)"));
        visitor.Visit(SqlAssert.Expr("set(multiset[1] multiset intersect distinct multiset[2])"));
        visitor.Visit(SqlAssert.Expr(CardinalityCall));
        visitor.Visit(SqlAssert.Expr(ElementCall));
        visitor.Visit(SqlAssert.Expr("absent on null"));
        visitor.Visit(SqlAssert.Select("select * from unnest(array[1]) with ordinality as x (a, n)"));
        visitor.Visit(SqlAssert.Select("select a into x from only (t) as o, lateral (select 1) as l, table(gen(1)) as f, schema.gen(1) tablesample system (5) repeatable (1), (values (1)) as v"));
        visitor.Visit(SqlAssert.Select("select grouping(a) from t group by rollup (a), cube (b), grouping sets ((), (a))"));
        visitor.Visit(SqlAssert.Select("select a from t order by a desc nulls last offset 1 row fetch first 2 percent rows with ties"));
        visitor.Visit(SqlAssert.Select("select sum(rank() over (order by a)) over (partition by b rows between 1 preceding and unbounded following exclude group) from t window w as (partition by a), w2 as (w order by b) for share"));
        visitor.Visit(SqlAssert.Parse<WithQuery>("with recursive general cte as (select 1) select * from cte"));
    }

    private const string CardinalityCall = "cardinality(array[1])";
    private const string ElementCall = "element(multiset[1])";

    private static Query ParseQuery(string sql)
    {
        var result = Sql.Parse(sql);
        Assert.Null(result.Error);
        Assert.NotNull(result.Root);
        return result.Root;
    }
}
