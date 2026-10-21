namespace QlParse.Tests;

// SQL Server query expressions (stream C): TOP, variable assignment and `alias =` select items, SELECT INTO,
// GROUP BY ALL / WITH ROLLUP, OFFSET / FETCH, WINDOW, COMPUTE, FOR XML / JSON / BROWSE, OPTION query hints and
// WITH XMLNAMESPACES. FROM items are in TsqlDmlTests.From.cs, DML statements in TsqlDmlTests.Modify.cs.
public sealed partial class TsqlDmlTests
{
    private const int Two = 2;
    private const int Three = 3;
    private const int Four = 4;
    private const int Five = 5;
    private const string Semicolon = ";";
    private const string AliasX = "x";

    private static string Text(string sql, SyntaxToken token) => sql[token.Position..token.Span.End];

    private static string SpanText(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string[] Texts(string sql, IEnumerable<SyntaxToken> tokens) => [.. tokens.Select(token => Text(sql, token))];

    private static T Ref<T>(T? value) where T : class
    {
        Assert.NotNull(value);
        return value;
    }

    private static T Ref<T>(T? value) where T : struct => Assert.NotNull(value);

    private static TsqlSelectExtras Extras(SelectStatement select) => Assert.IsType<TsqlSelectExtras>(select.SqlServer);

    // ---- TOP ----

    [Fact]
    public void Parses_parenthesized_top_with_percent_and_ties()
    {
        const string sql = "select top (10) percent with ties a from t order by a";
        var select = SqlAssert.SelectTsql(sql);
        var top = Assert.IsType<TsqlTopClause>(Extras(select).Top);
        Assert.Equal("10", SpanText(sql, top.Count.Span));
        Assert.NotNull(top.OpenParen);
        Assert.Equal("percent", Text(sql, Assert.NotNull(top.PercentKeyword)));
        Assert.Equal("ties", Text(sql, Assert.NotNull(top.TiesKeyword)));
        Assert.Equal("top (10) percent with ties", SpanText(sql, top.Span));
        Assert.Single(select.SelectList);
        Assert.Equal(sql, SpanText(sql, select.Span));
    }

    [Fact]
    public void Parses_legacy_top_without_parentheses()
    {
        const string sql = "select distinct top 5 * from t";
        var select = SqlAssert.SelectTsql(sql);
        Assert.NotNull(select.DistinctKeyword);
        var top = Assert.IsType<TsqlTopClause>(Extras(select).Top);
        Assert.Null(top.OpenParen);
        Assert.IsType<LiteralExpression>(top.Count);
        Assert.IsType<StarExpression>(select.SelectList[0].Expression);
    }

    [Fact]
    public void Top_takes_a_variable_expression()
    {
        var top = Assert.IsType<TsqlTopClause>(Extras(SqlAssert.SelectTsql("select top (@n + 1) a from t")).Top);
        Assert.IsType<BinaryExpression>(top.Count);
    }

    [Fact]
    public void Plain_select_allocates_no_sql_server_extras()
    {
        var select = SqlAssert.SelectTsql("select a, b from t where a = 1 order by b");
        Assert.Null(select.SqlServer);
        Assert.Null(select.Postgres);
    }

    // ---- Select items ----

    [Fact]
    public void Parses_variable_assignments_in_the_select_list()
    {
        const string sql = "select @a = a, @b += b from t";
        var select = SqlAssert.SelectTsql(sql);
        var first = Assert.IsType<TsqlVariableAssignmentExpression>(select.SelectList[0].Expression);
        Assert.Equal("@a", Text(sql, first.Variable));
        Assert.Equal(SyntaxKind.EqualsToken, first.Operator.Kind);
        Assert.IsType<IdentifierExpression>(first.Value);
        var second = Assert.IsType<TsqlVariableAssignmentExpression>(select.SelectList[1].Expression);
        Assert.Equal(SyntaxKind.CompoundAssignmentToken, second.Operator.Kind);
        Assert.Null(select.SelectList[1].Alias);
    }

    [Fact]
    public void Parses_equals_aliases_and_string_aliases()
    {
        const string sql = "select x = a + 1, 'y' = b, c as 'z', d 'w', e [f] from t";
        var items = SqlAssert.SelectTsql(sql).SelectList;
        Assert.Equal(AliasX, Text(sql, Assert.NotNull(items[0].Alias)));
        Assert.Equal(SyntaxKind.EqualsToken, Assert.NotNull(items[0].AsKeyword).Kind);
        Assert.IsType<BinaryExpression>(items[0].Expression);
        Assert.Equal("x = a + 1", SpanText(sql, items[0].Span));
        Assert.Equal(SyntaxKind.String, Assert.NotNull(items[1].Alias).Kind);
        Assert.Equal("'z'", Text(sql, Assert.NotNull(items[Two].Alias)));
        Assert.Equal(SyntaxKind.AsKeyword, Assert.NotNull(items[Two].AsKeyword).Kind);
        Assert.Equal("'w'", Text(sql, Assert.NotNull(items[Three].Alias)));
        Assert.Equal("[f]", Text(sql, Assert.NotNull(items[Four].Alias)));
    }

    [Fact]
    public void Equals_alias_is_a_comparison_without_sql_server()
    {
        var item = SqlAssert.Select("select x = 1 from t").SelectList[0];
        Assert.Null(item.Alias);
        Assert.IsType<BinaryExpression>(item.Expression);
    }

    // ---- SELECT INTO ----

    [Fact]
    public void Parses_select_into_a_new_table_on_a_filegroup()
    {
        const string sql = "select a into dbo.t on [primary] from u";
        var select = SqlAssert.SelectTsql(sql);
        Assert.Equal("into", Text(sql, Assert.NotNull(select.IntoKeyword)));
        Assert.Null(select.IntoTargets);
        var extras = Extras(select);
        Assert.Equal(["dbo", "t"], Texts(sql, Ref(extras.IntoTable).NameParts));
        Assert.Equal("[primary]", Text(sql, Assert.NotNull(extras.IntoFilegroup)));
        Assert.IsType<TableReference>(select.From);
    }

    [Fact]
    public void Parses_select_into_a_temporary_table()
    {
        const string sql = "select * into #t from u";
        var extras = Extras(SqlAssert.SelectTsql(sql));
        Assert.Equal(["#t"], Texts(sql, Ref(extras.IntoTable).NameParts));
        Assert.Null(extras.IntoOnKeyword);
    }

    // ---- GROUP BY ALL / WITH ROLLUP | CUBE, COMPUTE ----

    [Fact]
    public void Parses_group_by_all()
    {
        const string sql = "select a, count(*) from t group by all a";
        var select = SqlAssert.SelectTsql(sql);
        Assert.Equal("all", Text(sql, Assert.NotNull(Extras(select).GroupByAllKeyword)));
        Assert.Single(Ref(select.GroupBy).Keys);
    }

    [Theory]
    [InlineData("select a, b, count(*) from t group by a, b with rollup", "rollup")]
    [InlineData("select a, count(*) from t group by a with cube", "cube")]
    public void Parses_group_by_with_rollup_or_cube(string sql, string option)
    {
        var select = SqlAssert.SelectTsql(sql);
        var extras = Extras(select);
        Assert.Equal(option, Text(sql, Assert.NotNull(extras.GroupByWithOption)));
        Assert.EndsWith(option, SpanText(sql, Ref(select.GroupBy).Span), StringComparison.Ordinal);
    }

    [Fact]
    public void Parses_compute_clauses()
    {
        const string sql = "select a, b from t order by a compute sum(b), max(b) by a compute sum(b)";
        var compute = Extras(SqlAssert.SelectTsql(sql)).Compute;
        Assert.Equal(Two, compute.Count);
        Assert.Equal(Two, compute[0].Functions.Count);
        Assert.Single(compute[0].By);
        Assert.Null(compute[1].ByKeyword);
        Assert.Equal("compute sum(b)", SpanText(sql, compute[1].Span));
    }

    // ---- OFFSET / FETCH, WINDOW ----

    [Fact]
    public void Parses_offset_fetch()
    {
        var select = SqlAssert.SelectTsql("select a from t order by a offset 10 rows fetch next 5 rows only");
        Assert.NotNull(select.Offset);
        var fetch = Assert.IsType<FetchClause>(select.Fetch);
        Assert.Null(fetch.TiesKeyword);
        Assert.Null(select.SqlServer);
    }

    [Theory]
    [InlineData("select a from t order by a offset 1 rows fetch next 5 rows with ties")]
    [InlineData("select a from t order by a offset 1 rows fetch next 5 percent rows only")]
    [InlineData("select a from t order by a fetch first 5 rows only")]
    public void Rejects_fetch_forms_sql_server_lacks(string sql)
    {
        SqlAssert.ParseFails(sql, SqlOptions.SqlServer);
    }

    [Fact]
    public void Fetch_after_a_select_without_offset_starts_a_new_statement()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("select a from t order by a fetch next from c");
        Assert.Equal(Two, batch.Statements.Count);
        Assert.Null(Assert.IsType<SelectStatement>(batch.Statements[0]).Fetch);
    }

    [Fact]
    public void Parses_window_clause()
    {
        var select = SqlAssert.SelectTsql("select sum(a) over w from t window w as (partition by b)");
        Assert.Single(Ref(select.Window).Windows);
    }

    // ---- FOR XML / JSON / BROWSE ----

    [Fact]
    public void Parses_for_xml_with_directives()
    {
        const string sql = "select a from t for xml raw('row'), root('rows'), elements xsinil, binary base64, type";
        var forClause = Assert.IsType<TsqlForClause>(Extras(SqlAssert.SelectTsql(sql)).For);
        Assert.Equal("xml", Text(sql, forClause.Format));
        Assert.Equal(Five, forClause.Options.Count);
        Assert.Equal("raw", Text(sql, forClause.Options[0].Keywords[0]));
        Assert.Equal("'row'", SpanText(sql, Ref(forClause.Options[0].Argument).Span));
        Assert.Equal(["elements", "xsinil"], Texts(sql, forClause.Options[Two].Keywords));
        Assert.Equal(["binary", "base64"], Texts(sql, forClause.Options[Three].Keywords));
        Assert.Null(forClause.Options[Four].OpenParen);
    }

    [Theory]
    [InlineData("select a from t for xml auto, xmlschema", Two)]
    [InlineData("select a from t for xml explicit", 1)]
    [InlineData("select a from t for xml path, elements absent", Two)]
    [InlineData("select a from t for json path, root('r'), include_null_values, without_array_wrapper", Four)]
    [InlineData("select a from t for json auto", 1)]
    public void Parses_for_xml_and_json_modes(string sql, int options)
    {
        var forClause = Assert.IsType<TsqlForClause>(Extras(SqlAssert.SelectTsql(sql)).For);
        Assert.Equal(options, forClause.Options.Count);
    }

    [Fact]
    public void Parses_for_browse()
    {
        const string sql = "select a from t for browse";
        var forClause = Assert.IsType<TsqlForClause>(Extras(SqlAssert.SelectTsql(sql)).For);
        Assert.Equal("browse", Text(sql, forClause.Format));
        Assert.Empty(forClause.Options);
    }

    [Fact]
    public void Parses_for_xml_path_in_a_scalar_subquery()
    {
        var call = SqlAssert.ExprTsql<FunctionCallExpression>("stuff((select ',' + name from t for xml path('')), 1, 1, '')");
        var subquery = Assert.IsType<ScalarSubqueryExpression>(call.Arguments[0]);
        var select = Assert.IsType<SelectStatement>(subquery.Query);
        Assert.NotNull(Extras(select).For);
    }

    [Fact]
    public void For_update_stays_a_lock_clause()
    {
        var select = SqlAssert.SelectTsql("select a from t for update of a");
        Assert.NotNull(select.Lock);
        Assert.Null(select.SqlServer);
    }

    [Fact]
    public void For_xml_is_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("select a from t for xml path");
    }

    // ---- OPTION (query hints) ----

    [Fact]
    public void Parses_option_query_hints()
    {
        const string sql = "select a from t option (recompile, maxdop 4, optimize for (@p = 1, @q unknown), "
            + "use hint ('A', 'B'), label = 'x', hash group, optimize for unknown)";
        var option = Assert.IsType<TsqlOptionClause>(Extras(SqlAssert.SelectTsql(sql)).Option);
        var hints = option.Hints;
        Assert.Equal(7, hints.Count);
        Assert.Equal(["recompile"], Texts(sql, hints[0].Keywords));
        Assert.Equal("4", SpanText(sql, Ref(hints[1].Value).Span));
        Assert.Equal(["optimize", "for"], Texts(sql, hints[Two].Keywords));
        Assert.Equal("1", SpanText(sql, Ref(hints[Two].Arguments[0].Assigned).Span));
        Assert.Equal("unknown", Text(sql, Assert.NotNull(hints[Two].Arguments[1].Modifier)));
        Assert.Equal(Two, hints[Three].Arguments.Count);
        Assert.NotNull(hints[Four].EqualsToken);
        Assert.Equal(["hash", "group"], Texts(sql, hints[Five].Keywords));
        Assert.Equal(["optimize", "for", "unknown"], Texts(sql, hints[6].Keywords));
    }

    [Fact]
    public void Option_ends_a_union()
    {
        var union = SqlAssert.ParseTsql<SetOperation>("select a from t union all select b from u option (merge union)");
        Assert.NotNull(Extras(Assert.IsType<SelectStatement>(union.Right)).Option);
    }

    // ---- WITH XMLNAMESPACES ----

    [Fact]
    public void Parses_with_xmlnamespaces_before_a_query()
    {
        const string sql = "with xmlnamespaces ('urn:x' as x, default 'urn:d') select a from t for xml path('x:r')";
        var query = SqlAssert.ParseTsql<TsqlWithXmlNamespacesQuery>(sql);
        Assert.Equal(Two, query.Namespaces.Count);
        Assert.Equal(AliasX, Text(sql, Assert.NotNull(query.Namespaces[0].Prefix)));
        Assert.Equal("'urn:d'", Text(sql, query.Namespaces[1].Uri));
        Assert.NotNull(query.Namespaces[1].DefaultKeyword);
        Assert.Empty(query.Ctes);
        Assert.IsType<SelectStatement>(query.Query);
    }

    [Fact]
    public void Parses_with_xmlnamespaces_followed_by_ctes()
    {
        var query = SqlAssert.ParseTsql<TsqlWithXmlNamespacesQuery>("with xmlnamespaces ('urn:x' as x), c as (select 1 as a) select a from c");
        Assert.NotNull(query.Comma);
        Assert.Single(query.Ctes);
    }

    // ---- Visitor ----

    private sealed class TsqlRecorder : SqlVisitor
    {
        public HashSet<Type> Types { get; } = [];

        public override void VisitIdentifierExpression(IdentifierExpression node) => Types.Add(node.GetType());

        public override void VisitTsqlSelectExtras(TsqlSelectExtras node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlSelectExtras(node);
        }

        public override void VisitTsqlHint(TsqlHint node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlHint(node);
        }

        public override void VisitTsqlSetClause(TsqlSetClause node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlSetClause(node);
        }

        public override void VisitTsqlOutputClause(TsqlOutputClause node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlOutputClause(node);
        }

        public override void VisitTsqlRowsetFunctionTable(TsqlRowsetFunctionTable node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlRowsetFunctionTable(node);
        }

        public override void VisitTsqlPivotTable(TsqlPivotTable node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlPivotTable(node);
        }

        public override void VisitTsqlVariableAssignmentExpression(TsqlVariableAssignmentExpression node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlVariableAssignmentExpression(node);
        }

        public override void VisitTsqlMergeUpdateAction(TsqlMergeUpdateAction node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlMergeUpdateAction(node);
        }

        public override void VisitTsqlTruncateExtras(TsqlTruncateExtras node)
        {
            Types.Add(node.GetType());
            base.VisitTsqlTruncateExtras(node);
        }
    }

    [Theory]
    [InlineData("select top (1) a from t option (maxdop 1)", typeof(TsqlSelectExtras))]
    [InlineData("select a from t with (index(ix))", typeof(TsqlHint))]
    [InlineData("update t set a = b", typeof(TsqlSetClause))]
    [InlineData("delete t output deleted.a", typeof(TsqlOutputClause))]
    [InlineData("select * from openjson(@j) with (a int)", typeof(TsqlRowsetFunctionTable))]
    [InlineData("select * from s pivot (sum(v) for m in ([1])) p", typeof(TsqlPivotTable))]
    [InlineData("select @v = a from t", typeof(TsqlVariableAssignmentExpression))]
    [InlineData("merge t using s on t.a = s.a when matched then update set a = s.a;", typeof(TsqlMergeUpdateAction))]
    [InlineData("truncate table t with (partitions (1 to 2))", typeof(TsqlTruncateExtras))]
    public void Visitor_walks_sql_server_query_and_dml_nodes(string sql, Type expected)
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        Assert.Null(result.Error);
        var visitor = new TsqlRecorder();
        visitor.Visit(Assert.IsAssignableFrom<Query>(result.Root));
        Assert.Contains(expected, visitor.Types);
    }

    [Theory]
    [InlineData("select top 10 percent x = a, 'y' = b, @v = c from t group by all a with rollup order by a compute sum(b) for xml path(''), type option (recompile)")]
    [InlineData("select * from t tablesample (10 percent) with (nolock) cross apply f(t.a) g outer apply (select 1 as x) h inner hash join u on 1 = 1")]
    [InlineData("select * from s pivot (sum(v) for m in ([1], [2])) p unpivot (w for n in ([1], [2])) u")]
    [InlineData("select * from t for system_time contained in (1, 2) h")]
    [InlineData("select * from openjson(@j, '$') with (a int '$.a' as json) j, openrowset(bulk 'f', single_blob) b")]
    [InlineData("select * from opendatasource('p', 'c').db.dbo.t x, containstable(t, *, 'w', language 1033) k")]
    [InlineData("with xmlnamespaces (default 'urn:d'), c as (select 1 as a) select a from c")]
    [InlineData("insert top (1) into t with (tablock) (a) output inserted.a into @t select 1 option (maxdop 1)")]
    [InlineData("insert t default values")]
    [InlineData("update top (1) t set @v = a = 1, b += 2, c = default, d.write('x', 0, 1) output inserted.* from t join u on 1 = 1 where current of global c")]
    [InlineData("delete top (1) from t output deleted.* from t join u on 1 = 1 where a = 1 option (recompile)")]
    [InlineData("merge top (1) into t with (holdlock) as x using s on x.a = s.a when matched then delete when not matched then insert default values output $action;")]
    [InlineData("bulk insert t from 'f' with (fieldterminator = ',', order (a desc), tablock)")]
    [InlineData("truncate table t with (partitions (1, 2 to 3))")]
    public void Default_visitor_walks_every_sql_server_query_and_dml_node(string sql)
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        Assert.Null(result.Error);
        new SqlVisitor().Visit(Assert.IsAssignableFrom<Query>(result.Root));
    }
}
