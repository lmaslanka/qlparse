namespace QlParse.Tests;

// SQL Server graph tables and queries (stream D): AS NODE / AS EDGE, edge constraints, pseudo-columns,
// MATCH patterns, SHORTEST_PATH, LAST_NODE, FOR PATH and WITHIN GROUP (GRAPH PATH).
public sealed partial class TsqlDdlTests
{
    private static TsqlMatchExpression Match(string sql)
    {
        var select = SqlAssert.SelectTsql(sql);
        new SqlVisitor().Visit(select);
        return Assert.IsType<TsqlMatchExpression>(select.Where!.Expression);
    }

    [Fact]
    public void Parses_node_and_edge_tables()
    {
        const string Node = "create table Person (id int primary key, name varchar(100)) as node";
        var node = ParseWhole<CreateTableStatement>(Node);
        Assert.Equal(As, Text(Node, node.SqlServer!.AsKeyword));
        Assert.Equal("node", Text(Node, node.SqlServer.TableKind));
        const string Edge = "create table likes as edge on fg";
        var edge = ParseWhole<CreateTableStatement>(Edge);
        Assert.Null(edge.Elements);
        Assert.Equal("edge", Text(Edge, edge.SqlServer!.TableKind));
        Assert.Equal(FileGroup, Text(Edge, edge.SqlServer.Storage!.Target));
        SqlAssert.ParseFails(Node);
        SqlAssert.ParseFails(Edge);
    }

    [Fact]
    public void Parses_edge_constraint()
    {
        const string Sql = "create table friendOf (since date, constraint ec connection (Person to Person, dbo.Person to City) on delete cascade) as edge";
        var constraint = Assert.IsType<TableConstraint>(ParseWhole<CreateTableStatement>(Sql).Elements![1]);
        Assert.Equal("ec", Text(Sql, constraint.ConstraintName));
        var connections = constraint.SqlServer!.Connection!.Connections;
        Assert.Equal(Count.Two, connections.Count);
        Assert.Equal("dbo Person", Joined(Sql, connections[1].FromNode));
        Assert.Equal("City", Text(Sql, connections[1].ToNode[0]));
        Assert.Equal("cascade", Text(Sql, Assert.Single(constraint.Actions).Action));
    }

    [Fact]
    public void Parses_add_edge_constraint()
    {
        const string Sql = "alter table friendOf add constraint ec connection (Person to Person)";
        var constraint = AlterAction<AddConstraintAction>(Sql).Constraint;
        Assert.NotNull(constraint.SqlServer!.Connection);
        Assert.Empty(constraint.Actions);
    }

    [Fact]
    public void Graph_pseudo_columns_are_ordinary_identifiers()
    {
        const string Sql = "select $node_id, $edge_id, $from_id, $to_id from friendOf";
        var select = SqlAssert.SelectTsql(Sql);
        Assert.Equal("$to_id", Text(Sql, Assert.IsType<IdentifierExpression>(select.SelectList[Fourth].Expression).Identifier));
    }

    [Fact]
    public void Parses_match_with_forward_and_backward_edges()
    {
        const string Sql = "select 1 from a, e, b, e2, c where match(a-(e)->b<-(e2)-c and b-(e)->c)";
        var match = Match(Sql);
        Assert.Equal(Count.Two, match.Patterns.Count);
        Assert.Equal("and", Text(Sql, Assert.Single(match.AndKeywords)));
        var elements = match.Patterns[0].Elements;
        Assert.Equal(Count.Five, elements.Count);
        var forward = Assert.IsType<TsqlGraphEdge>(elements[1]);
        Assert.Null(forward.LessThan);
        Assert.Equal("->", Text(Sql, forward.Arrow));
        var backward = Assert.IsType<TsqlGraphEdge>(elements[Fourth]);
        Assert.Equal("<", Text(Sql, backward.LessThan));
        Assert.Equal("e2", Text(Sql, backward.Name));
        Assert.Equal(ColumnC, Text(Sql, Assert.IsType<TsqlGraphNode>(elements[Fifth]).Name));
        Assert.Null(match.Patterns[0].ShortestPathKeyword);
    }

    [Fact]
    public void Match_combines_with_other_predicates()
    {
        const string Sql = "select 1 from a, e, b where match(a-(e)->b) and a.id = 1";
        var where = Assert.IsType<BinaryExpression>(SqlAssert.SelectTsql(Sql).Where!.Expression);
        Assert.IsType<TsqlMatchExpression>(where.Left);
    }

    [Fact]
    public void Parses_shortest_path_with_plus_quantifier_and_graph_path_aggregate()
    {
        const string Sql = "select p1.name, string_agg(p2.name, '->') within group (graph path) as friends "
            + "from Person as p1, friendOf for path as fo, Person for path as p2 where match(shortest_path(p1(-(fo)->p2)+))";
        var select = SqlAssert.SelectTsql(Sql);
        new SqlVisitor().Visit(select);
        var aggregate = Assert.IsType<TsqlGraphPathAggregateExpression>(select.SelectList[1].Expression);
        Assert.Equal("string_agg", Text(Sql, Assert.IsType<FunctionCallExpression>(aggregate.Call).Name));
        Assert.Equal("graph", Text(Sql, aggregate.GraphKeyword));
        Assert.Equal(Count.Two, select.ExtraFrom.Count);
        var match = Assert.IsType<TsqlMatchExpression>(select.Where!.Expression);
        var pattern = Assert.Single(match.Patterns);
        Assert.Equal("shortest_path", Text(Sql, pattern.ShortestPathKeyword));
        var group = Assert.IsType<TsqlGraphGroup>(pattern.Elements[1]);
        Assert.Equal("+", Text(Sql, Assert.Single(group.Quantifier)));
        Assert.Equal(Count.Two, group.Elements.Count);
    }

    [Fact]
    public void Parses_for_path_table_items()
    {
        const string Sql = "select 1 from friendOf for path as fo, Person for path p2";
        var select = SqlAssert.SelectTsql(Sql);
        new SqlVisitor().Visit(select);
        var first = Assert.IsType<TsqlGraphPathTable>(select.From);
        Assert.Equal("fo", Text(Sql, first.Alias));
        Assert.Equal(As, Text(Sql, first.AsKeyword));
        var second = Assert.IsType<TsqlGraphPathTable>(Assert.Single(select.ExtraFrom).Table);
        Assert.Null(second.AsKeyword);
        Assert.Equal(P2, Text(Sql, second.Alias));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_bounded_quantifier_and_last_node()
    {
        const string Sql = "select last_value(p2.name) within group (graph path) from Person p1, friendOf for path fo, Person for path p2, likes l, r "
            + "where match(shortest_path(p1(-(fo)->p2){1,3}) and last_node(p2)-(l)->r)";
        var match = Match(Sql);
        var group = Assert.IsType<TsqlGraphGroup>(match.Patterns[0].Elements[1]);
        Assert.Equal("{ 1 , 3 }", Joined(Sql, group.Quantifier));
        var lastNode = Assert.IsType<TsqlGraphNode>(match.Patterns[1].Elements[0]);
        Assert.Equal("last_node", Text(Sql, lastNode.LastNodeKeyword));
        Assert.Equal(P2, Text(Sql, lastNode.Name));
    }

    [Fact]
    public void Parses_shortest_path_with_leading_group()
    {
        const string Sql = "select 1 from Person p1, friendOf for path fo, Person for path p2 where match(shortest_path((p2<-(fo)-)+p1))";
        var elements = Assert.Single(Match(Sql).Patterns).Elements;
        var group = Assert.IsType<TsqlGraphGroup>(elements[0]);
        Assert.IsType<TsqlGraphEdge>(group.Elements[1]);
        Assert.Equal("p1", Text(Sql, Assert.IsType<TsqlGraphNode>(elements[1]).Name));
    }

    [Theory]
    [InlineData("select 1 from a, e, b where match(a-(e)->b)")]
    [InlineData("select 1 from friendOf for path fo")]
    [InlineData("select string_agg(a, ',') within group (graph path) from t")]
    public void Rejects_graph_queries_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Theory]
    [InlineData("select 1 from a where match(a-(e)-b)")]
    [InlineData("select 1 from a where match(shortest_path(a(-(e)->b)))")]
    [InlineData("select 1 from a where match()")]
    public void Rejects_malformed_match_patterns(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);
}
