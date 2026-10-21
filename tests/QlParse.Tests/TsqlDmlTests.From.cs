namespace QlParse.Tests;

// SQL Server FROM items (stream C): table hints, join hints, CROSS / OUTER APPLY, PIVOT / UNPIVOT, TABLESAMPLE,
// FOR SYSTEM_TIME, multi-part and omitted-part names, table variables, derived tables, table-valued and rowset
// functions, and system catalog names.
public sealed partial class TsqlDmlTests
{
    private static TableSource From(string sql) => Assert.IsAssignableFrom<TableSource>(SqlAssert.SelectTsql(sql).From);

    // ---- Table hints ----

    [Fact]
    public void Parses_table_hints_after_the_alias()
    {
        const string sql = "select * from t as x with (nolock, index(ix1, ix2), readpast) where a = 1";
        var hinted = Assert.IsType<TsqlHintedTable>(From(sql));
        var table = Assert.IsType<TableReference>(hinted.Table);
        Assert.Equal(AliasX, Text(sql, Assert.NotNull(table.Alias)));
        Assert.Equal(Three, hinted.Hints.Count);
        Assert.Equal("nolock", Text(sql, hinted.Hints[0].Keywords[0]));
        Assert.Equal(Two, hinted.Hints[1].Arguments.Count);
        Assert.Equal("t as x with (nolock, index(ix1, ix2), readpast)", SpanText(sql, hinted.Span));
    }

    [Fact]
    public void Table_hints_need_no_commas()
    {
        var hinted = Assert.IsType<TsqlHintedTable>(From("select * from t x with (updlock holdlock)"));
        Assert.Equal(Two, hinted.Hints.Count);
    }

    [Fact]
    public void Parses_valued_table_hints()
    {
        const string sql = "select * from t with (index = ix, forceseek(ix (a, b)), spatial_window_max_cells = 512)";
        var hints = Assert.IsType<TsqlHintedTable>(From(sql)).Hints;
        Assert.Equal("ix", SpanText(sql, Ref(hints[0].Value).Span));
        Assert.Single(hints[1].Arguments);
        Assert.Equal("512", SpanText(sql, Ref(hints[Two].Value).Span));
    }

    [Fact]
    public void With_check_option_after_a_view_query_is_not_a_table_hint()
    {
        var view = SqlAssert.ParseTsql<CreateViewStatement>("create view v as select a from t with check option");
        Assert.IsType<TableReference>(Assert.IsType<SelectStatement>(view.Query).From);
    }

    [Fact]
    public void Table_hints_are_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("select * from t with (nolock)");
    }

    // ---- Joins ----

    [Fact]
    public void Parses_join_hints()
    {
        const string sql = "select * from a inner hash join b on a.id = b.id left outer merge join c on c.id = b.id "
            + "full loop join d on 1 = 1 inner remote join e on 1 = 1";
        var joins = SqlAssert.SelectTsql(sql).Joins;
        Assert.Equal(["hash", "merge", "loop", "remote"], joins.Select(join => Text(sql, Ref(join.SqlServer).Hint)));
        Assert.IsType<OnConstraint>(joins[0].Constraint);
        Assert.NotNull(joins[1].OuterKeyword);
        Assert.Equal("left outer merge join c on c.id = b.id", SpanText(sql, joins[1].Span));
    }

    [Fact]
    public void Unhinted_join_has_no_sql_server_extras()
    {
        var join = Assert.Single(SqlAssert.SelectTsql("select * from a join b on a.x = b.x").Joins);
        Assert.Null(join.SqlServer);
        Assert.IsType<OnConstraint>(join.Constraint);
    }

    [Fact]
    public void Parses_cross_and_outer_apply()
    {
        const string sql = "select * from a cross apply f(a.x) as g outer apply (select top 1 * from b where b.id = a.id) as h";
        var joins = SqlAssert.SelectTsql(sql).Joins;
        Assert.Equal(SyntaxKind.CrossKeyword, Assert.NotNull(joins[0].JoinType).Kind);
        Assert.Equal("apply", Text(sql, joins[0].JoinKeyword));
        Assert.Null(joins[0].Constraint);
        Assert.IsType<TableFunction>(joins[0].Table);
        Assert.Equal(SyntaxKind.OuterKeyword, Assert.NotNull(joins[1].JoinType).Kind);
        Assert.IsType<DerivedTable>(joins[1].Table);
    }

    [Fact]
    public void Outer_apply_follows_a_comma_join()
    {
        var extra = Assert.Single(SqlAssert.SelectTsql("select * from a, b outer apply f(b.x) g").ExtraFrom);
        Assert.Single(extra.Joins);
    }

    [Theory]
    [InlineData("select * from a cross apply f(a.x) g")]
    [InlineData("select * from a outer apply f(a.x) g")]
    [InlineData("select * from a inner hash join b on 1 = 1")]
    public void Apply_and_join_hints_are_rejected_without_sql_server(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    // ---- PIVOT / UNPIVOT ----

    [Fact]
    public void Parses_pivot()
    {
        const string sql = "select * from (select y, m, v from s) as src pivot (sum(v) for m in ([1], [2], [3])) as p";
        var pivot = Assert.IsType<TsqlPivotTable>(From(sql));
        Assert.IsType<DerivedTable>(pivot.Table);
        Assert.IsType<FunctionCallExpression>(pivot.Aggregate);
        Assert.IsType<IdentifierExpression>(pivot.Column);
        Assert.Equal(["[1]", "[2]", "[3]"], Texts(sql, pivot.Values));
        Assert.Equal("p", Text(sql, pivot.Alias));
    }

    [Fact]
    public void Parses_unpivot()
    {
        const string sql = "select * from p unpivot (v for m in (a, b)) u";
        var unpivot = Assert.IsType<TsqlUnpivotTable>(From(sql));
        Assert.Equal("v", Text(sql, unpivot.ValueColumn));
        Assert.Equal("m", Text(sql, unpivot.NameColumn));
        Assert.Equal(Two, unpivot.Columns.Count);
        Assert.Null(unpivot.AsKeyword);
    }

    [Fact]
    public void Pivot_requires_an_alias()
    {
        SqlAssert.ParseFails("select * from s pivot (sum(v) for m in ([1]))", SqlOptions.SqlServer);
    }

    // ---- TABLESAMPLE and FOR SYSTEM_TIME ----

    [Fact]
    public void Parses_tablesample_with_percent()
    {
        const string sql = "select * from t tablesample (10 percent)";
        var sample = Assert.IsType<TsqlSampledTable>(From(sql));
        Assert.Null(sample.SystemKeyword);
        Assert.Equal("percent", Text(sql, Assert.NotNull(sample.Unit)));
    }

    [Fact]
    public void Parses_tablesample_system_rows_repeatable_then_hints()
    {
        const string sql = "select * from t as x tablesample system (100 rows) repeatable (42) with (nolock)";
        var hinted = Assert.IsType<TsqlHintedTable>(From(sql));
        var sample = Assert.IsType<TsqlSampledTable>(hinted.Table);
        Assert.NotNull(sample.SystemKeyword);
        Assert.Equal("rows", Text(sql, Assert.NotNull(sample.Unit)));
        Assert.Equal("42", SpanText(sql, Ref(sample.Seed).Span));
    }

    [Fact]
    public void Tablesample_keeps_the_iso_form_without_sql_server()
    {
        Assert.IsType<SampledTable>(SqlAssert.Select("select * from t tablesample bernoulli (10)").From);
        SqlAssert.ParseFails("select * from t tablesample (10 percent)");
    }

    [Fact]
    public void Parses_for_system_time_contained_in()
    {
        const string sql = "select * from t for system_time contained in ('2020-01-01', '2021-01-01') as h";
        var temporal = Assert.IsType<TsqlTemporalTable>(From(sql));
        Assert.Null(Assert.IsType<TableReference>(temporal.Table).SystemTime);
        Assert.Equal("'2020-01-01'", SpanText(sql, temporal.Start.Span));
        Assert.Equal("h", Text(sql, Assert.NotNull(temporal.Alias)));
        Assert.Equal("t for system_time contained in ('2020-01-01', '2021-01-01') as h", SpanText(sql, temporal.Span));
    }

    [Theory]
    [InlineData("select * from t for system_time as of '2020-01-01'")]
    [InlineData("select * from t for system_time from '2020-01-01' to '2021-01-01' x")]
    [InlineData("select * from t for system_time between '2020-01-01' and '2021-01-01'")]
    [InlineData("select * from t for system_time all")]
    public void Other_for_system_time_forms_stay_iso(string sql)
    {
        Assert.NotNull(Assert.IsType<TableReference>(From(sql)).SystemTime);
    }

    [Fact]
    public void Contained_in_is_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("select * from t for system_time contained in (1, 2)");
    }

    // ---- Names ----

    [Theory]
    [InlineData("select * from srv.db.dbo.t", new[] { "srv", "db", "dbo", "t" })]
    [InlineData("select * from db..t", new[] { "db", "", "t" })]
    [InlineData("select * from srv...t", new[] { "srv", "", "", "t" })]
    [InlineData("select * from .dbo.t", new[] { "", "dbo", "t" })]
    [InlineData("select * from sys.objects", new[] { "sys", "objects" })]
    [InlineData("select * from information_schema.tables", new[] { "information_schema", "tables" })]
    [InlineData("select * from master.sys.dm_exec_requests", new[] { "master", "sys", "dm_exec_requests" })]
    public void Parses_multi_part_names(string sql, string[] parts)
    {
        var table = Assert.IsType<TableReference>(From(sql));
        Assert.Equal(parts, Texts(sql, table.NameParts));
        Assert.All(table.NameParts, part => Assert.Equal(SyntaxKind.Identifier, part.Kind));
    }

    [Fact]
    public void Omitted_name_parts_are_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("select * from db..t");
    }

    [Fact]
    public void Parses_table_variables_and_temporary_tables()
    {
        const string sql = "select * from @t as x join #tmp y on x.a = y.a";
        var select = SqlAssert.SelectTsql(sql);
        var variable = Assert.IsType<TableReference>(select.From);
        Assert.Equal(SyntaxKind.EmbeddedHost, variable.NameParts[0].Kind);
        Assert.Equal("#tmp", Text(sql, Assert.IsType<TableReference>(select.Joins[0].Table).NameParts[0]));
    }

    // ---- Derived tables ----

    [Fact]
    public void Parses_values_derived_table_with_column_aliases()
    {
        const string sql = "select * from (values (1, 2), (3, 4)) as v (a, b)";
        var derived = Assert.IsType<DerivedTable>(From(sql));
        Assert.Equal(Two, Assert.IsType<ValuesQuery>(derived.Query).Rows.Count);
        Assert.Equal(["a", "b"], Texts(sql, Ref(derived.Columns)));
    }

    [Fact]
    public void Parses_derived_table_column_aliases_without_as()
    {
        var derived = Assert.IsType<DerivedTable>(From("select * from (select 1 as x) d (y)"));
        Assert.Null(derived.AsKeyword);
        Assert.Single(Ref(derived.Columns));
    }

    // ---- Table-valued and rowset functions ----

    [Theory]
    [InlineData("select * from generate_series(1, 10)", Two)]
    [InlineData("select * from generate_series(1, 10, 2) g", Three)]
    [InlineData("select * from dbo.fn(1, 'x') as f", Two)]
    [InlineData("select * from string_split('a,b', ',')", Two)]
    public void Parses_table_valued_function_calls(string sql, int arguments)
    {
        Assert.Equal(arguments, Assert.IsType<TableFunction>(From(sql)).Arguments.Count);
    }

    [Fact]
    public void Table_valued_function_alias_takes_column_names()
    {
        const string sql = "select * from dbo.fn(1) f (a, b) where a = 1";
        var function = Assert.IsType<TableFunction>(From(sql));
        Assert.Equal(["dbo", "fn"], Texts(sql, function.NameParts));
        Assert.Equal(Two, Ref(function.Columns).Count);
    }

    [Fact]
    public void Parses_openjson_with_schema()
    {
        const string sql = "select * from openjson(@j, '$.items') with (id int '$.id', name varchar(100) '$.name', obj nvarchar(max) '$.o' as json) as j";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal("openjson", Text(sql, rowset.NameParts[0]));
        Assert.Equal(Two, rowset.Arguments.Count);
        var schema = Ref(rowset.Schema);
        Assert.Equal(Three, schema.Columns.Count);
        Assert.Equal("'$.id'", SpanText(sql, Ref(schema.Columns[0].Path).Span));
        Assert.NotNull(schema.Columns[Two].JsonKeyword);
        Assert.Equal("j", Text(sql, Assert.NotNull(rowset.Alias)));
    }

    [Fact]
    public void Parses_openjson_in_cross_apply()
    {
        var join = Assert.Single(SqlAssert.SelectTsql("select * from t cross apply openjson(t.doc) with (a int) j").Joins);
        Assert.Single(Ref(Assert.IsType<TsqlRowsetFunctionTable>(join.Table).Schema).Columns);
    }

    [Fact]
    public void Parses_openxml_with_schema_or_table()
    {
        var columns = Assert.IsType<TsqlRowsetFunctionTable>(From("select * from openxml(@h, '/root/c', 2) with (id int, name varchar(20) '@Name')"));
        Assert.Equal(Three, columns.Arguments.Count);
        Assert.Equal(Two, Ref(columns.Schema).Columns.Count);
        const string sql = "select * from openxml(@h, '/root/c') with dbo.t";
        var table = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal(["dbo", "t"], Texts(sql, Ref(table.Schema).TableName));
    }

    [Fact]
    public void Parses_openrowset_bulk_forms()
    {
        const string sql = "select * from openrowset(bulk 'c:\\x.csv', formatfile = 'c:\\x.fmt', firstrow = 2) as r";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal("bulk", Text(sql, Assert.NotNull(rowset.Arguments[0].Name)));
        Assert.Null(rowset.Arguments[0].EqualsToken);
        Assert.Equal("formatfile", Text(sql, Assert.NotNull(rowset.Arguments[1].Name)));
        Assert.NotNull(rowset.Arguments[1].EqualsToken);
        var blob = Assert.IsType<TsqlRowsetFunctionTable>(From("select * from openrowset(bulk N'f.txt', single_clob) as j"));
        Assert.IsType<IdentifierExpression>(blob.Arguments[1].Value);
        var parquet = Assert.IsType<TsqlRowsetFunctionTable>(From("select * from openrowset(bulk 'y.parquet', format = 'parquet') with (a int) as r"));
        Assert.NotNull(parquet.Schema);
    }

    [Fact]
    public void Parses_openrowset_provider_with_semicolon_separated_connection()
    {
        const string sql = "select * from openrowset('Microsoft.Jet.OLEDB.4.0', 'c:\\x.mdb';'admin';'', orders) as o";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal(Five, rowset.Arguments.Count);
        Assert.Equal([",", Semicolon, Semicolon, ","], Texts(sql, rowset.Separators));
    }

    [Fact]
    public void Parses_openquery_and_opendatasource()
    {
        Assert.Equal(Two, Assert.IsType<TsqlRowsetFunctionTable>(From("select * from openquery(srv, 'select 1') as q")).Arguments.Count);
        const string sql = "select * from opendatasource('SQLNCLI', 'Data Source=srv').db.dbo.t as x";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal(["db", "dbo", "t"], Texts(sql, rowset.ObjectName));
        Assert.Equal(AliasX, Text(sql, Assert.NotNull(rowset.Alias)));
    }

    [Fact]
    public void Parses_containstable_and_freetexttable()
    {
        const string sql = "select * from containstable(t, (a, b), 'word', language 1033, 10) as k";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal(Five, rowset.Arguments.Count);
        Assert.Equal("language", Text(sql, Assert.NotNull(rowset.Arguments[Three].Name)));
        var star = Assert.IsType<TsqlRowsetFunctionTable>(From("select * from containstable(t, *, 'word') as k"));
        Assert.IsType<StarExpression>(star.Arguments[1].Value);
        var freetext = SqlAssert.SelectTsql("select * from freetexttable(dbo.t, a, 'text') ft join t on t.id = ft.[key]");
        Assert.IsType<TsqlRowsetFunctionTable>(freetext.From);
    }

    [Theory]
    [InlineData("select * from semantickeyphrasetable(t, *) as k", Two)]
    [InlineData("select * from semanticsimilaritytable(t, a, 1) as s", Three)]
    [InlineData("select * from semanticsimilaritydetailstable(t, a, 1, b, 2) as s", Five)]
    public void Parses_semantic_table_functions(string sql, int arguments)
    {
        Assert.Equal(arguments, Assert.IsType<TsqlRowsetFunctionTable>(From(sql)).Arguments.Count);
    }

    [Fact]
    public void Parses_vector_search()
    {
        const string sql = "select * from vector_search(table = dbo.t as t, column = embedding, similar_to = @qv, metric = 'cosine', top_n = 10) as r";
        var rowset = Assert.IsType<TsqlRowsetFunctionTable>(From(sql));
        Assert.Equal(Five, rowset.Arguments.Count);
        Assert.Equal("table", Text(sql, Assert.NotNull(rowset.Arguments[0].Name)));
        Assert.Equal("t", Text(sql, Assert.NotNull(rowset.Arguments[0].Alias)));
        Assert.IsType<MemberAccessExpression>(rowset.Arguments[0].Value);
    }

    [Fact]
    public void Rowset_functions_are_plain_calls_without_sql_server()
    {
        Assert.IsType<TableFunction>(SqlAssert.Select("select * from openjson(j) x").From);
        SqlAssert.ParseFails("select * from openjson(j) with (a int)");
    }
}
