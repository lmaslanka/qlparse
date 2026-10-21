namespace QlParse.Tests;

// SQL Server expressions (stream B): built-in functions, conversions, AT TIME ZONE, aggregates with
// WITHIN GROUP, JSON / XML / CLR methods, AI functions, NEXT VALUE FOR ... OVER and the full-text
// predicates. Data types are in TsqlExpressionTests.Types.cs.
public sealed partial class TsqlExpressionTests
{
    private const string SelectPrefix = "select ";
    private const int Pair = 2;
    private const string AtTimeZone = "d at time zone 'UTC'";
    private const string NextValueOver = "next value for dbo.s over (order by a)";
    private const string StringAgg = "string_agg(a, ',') within group (order by a desc)";
    private const string ContainsColumns = "select a from t where contains((b, c), 'x', language 1033)";
    private const string FreetextStar = "select a from t where freetext(*, 'x')";
    private const string Embeddings = "ai_generate_embeddings(a use model dbo.m parameters '{}')";

    private static string ExprText(string expression, SourceSpan span) =>
        $"{SelectPrefix}{expression}"[span.Position..span.End];

    private static Expression WhereOf(string sql) => SqlAssert.SelectTsql(sql).Where!.Expression;

    private static FunctionCallExpression Call(string expression, string name, int arguments)
    {
        var call = SqlAssert.ExprTsql<FunctionCallExpression>(expression);
        Assert.Equal(name, ExprText(expression, call.Name.Span), ignoreCase: true);
        Assert.Equal(arguments, call.Arguments.Count);
        Assert.Equal(expression, ExprText(expression, call.Span));
        return call;
    }

    // ---- Functions that are plain calls ----

    [Theory]
    [InlineData("len(a)", "len", 1)]
    [InlineData("datalength(a)", "datalength", 1)]
    [InlineData("charindex('a', b, 2)", "charindex", 3)]
    [InlineData("patindex('%a%', b)", "patindex", 2)]
    [InlineData("stuff(a, 1, 2, 'x')", "stuff", 4)]
    [InlineData("replicate('a', 3)", "replicate", 2)]
    [InlineData("reverse(a)", "reverse", 1)]
    [InlineData("space(3)", "space", 1)]
    [InlineData("string_split('a,b', ',', 1)", "string_split", 3)]
    [InlineData("concat(a, b, c)", "concat", 3)]
    [InlineData("concat_ws(',', a, b)", "concat_ws", 3)]
    [InlineData("ltrim(a, 'x')", "ltrim", 2)]
    [InlineData("rtrim(a)", "rtrim", 1)]
    [InlineData("format(a, 'N2', 'en-US')", "format", 3)]
    [InlineData("quotename(a, '\"')", "quotename", 2)]
    [InlineData("soundex(a)", "soundex", 1)]
    [InlineData("difference(a, b)", "difference", 2)]
    [InlineData("string_escape(a, 'json')", "string_escape", 2)]
    [InlineData("regexp_like(a, '^x', 'i')", "regexp_like", 3)]
    [InlineData("regexp_replace(a, 'x', 'y')", "regexp_replace", 3)]
    [InlineData("regexp_substr(a, 'x')", "regexp_substr", 2)]
    [InlineData("regexp_instr(a, 'x')", "regexp_instr", 2)]
    [InlineData("regexp_count(a, 'x')", "regexp_count", 2)]
    [InlineData("regexp_matches(a, 'x')", "regexp_matches", 2)]
    [InlineData("regexp_split_to_table(a, ',')", "regexp_split_to_table", 2)]
    public void Parses_string_functions_as_calls(string expression, string name, int arguments)
    {
        Call(expression, name, arguments);
    }

    [Theory]
    [InlineData("select value from string_split('a,b', ',')", 2)]
    [InlineData("select value, ordinal from string_split(@s, ',', 1)", 3)]
    [InlineData("select * from regexp_split_to_table(@s, ',')", 2)]
    public void Table_valued_string_functions_are_table_functions(string sql, int arguments)
    {
        var function = Assert.IsType<TableFunction>(SqlAssert.SelectTsql(sql).From);
        Assert.Single(function.NameParts);
        Assert.Equal(arguments, function.Arguments.Count);
    }

    [Theory]
    [InlineData("getdate()", "getdate", 0)]
    [InlineData("getutcdate()", "getutcdate", 0)]
    [InlineData("sysdatetime()", "sysdatetime", 0)]
    [InlineData("sysutcdatetime()", "sysutcdatetime", 0)]
    [InlineData("sysdatetimeoffset()", "sysdatetimeoffset", 0)]
    [InlineData("eomonth(a, 1)", "eomonth", 2)]
    [InlineData("datefromparts(2020, 1, 2)", "datefromparts", 3)]
    [InlineData("datetime2fromparts(2020, 1, 2, 3, 4, 5, 0, 0)", "datetime2fromparts", 8)]
    [InlineData("datetimeoffsetfromparts(2020, 1, 2, 3, 4, 5, 0, 1, 0, 7)", "datetimeoffsetfromparts", 10)]
    [InlineData("timefromparts(1, 2, 3, 0, 0)", "timefromparts", 5)]
    [InlineData("smalldatetimefromparts(2020, 1, 2, 3, 4)", "smalldatetimefromparts", 5)]
    [InlineData("switchoffset(a, '-05:00')", "switchoffset", 2)]
    [InlineData("todatetimeoffset(a, '+01:00')", "todatetimeoffset", 2)]
    [InlineData("isdate(a)", "isdate", 1)]
    [InlineData("iif(a > 1, 'x', 'y')", "iif", 3)]
    [InlineData("choose(2, 'a', 'b', 'c')", "choose", 4)]
    [InlineData("isnull(a, 0)", "isnull", 2)]
    [InlineData("greatest(a, b, 1)", "greatest", 3)]
    [InlineData("least(a, b)", "least", 2)]
    [InlineData("json_modify(a, '$.x', 1)", "json_modify", 3)]
    [InlineData("isjson(a, object)", "isjson", 2)]
    [InlineData("json_path_exists(a, '$.x')", "json_path_exists", 2)]
    [InlineData("json_contains(a, 1, '$.x')", "json_contains", 3)]
    [InlineData("vector_distance('cosine', a, b)", "vector_distance", 3)]
    [InlineData("vector_norm(a, 'norm2')", "vector_norm", 2)]
    [InlineData("vector_normalize(a, 'norm2')", "vector_normalize", 2)]
    public void Parses_date_logical_json_and_vector_functions_as_calls(string expression, string name, int arguments)
    {
        Call(expression, name, arguments);
    }

    [Theory]
    [InlineData("count_big(*)", "count_big", 1)]
    [InlineData("checksum_agg(a)", "checksum_agg", 1)]
    [InlineData("checksum(a, b)", "checksum", 2)]
    [InlineData("binary_checksum(*)", "binary_checksum", 1)]
    [InlineData("stdev(a)", "stdev", 1)]
    [InlineData("stdevp(a)", "stdevp", 1)]
    [InlineData("var(a)", "var", 1)]
    [InlineData("varp(a)", "varp", 1)]
    [InlineData("approx_count_distinct(a)", "approx_count_distinct", 1)]
    [InlineData("grouping_id(a, b)", "grouping_id", 2)]
    [InlineData("rowcount_big()", "rowcount_big", 0)]
    [InlineData("scope_identity()", "scope_identity", 0)]
    [InlineData("ident_current('t')", "ident_current", 1)]
    [InlineData("error_number()", "error_number", 0)]
    [InlineData("error_message()", "error_message", 0)]
    [InlineData("error_severity()", "error_severity", 0)]
    [InlineData("error_state()", "error_state", 0)]
    [InlineData("error_line()", "error_line", 0)]
    [InlineData("error_procedure()", "error_procedure", 0)]
    [InlineData("object_id('dbo.t', 'U')", "object_id", 2)]
    [InlineData("object_name(1)", "object_name", 1)]
    [InlineData("db_id()", "db_id", 0)]
    [InlineData("db_name(1)", "db_name", 1)]
    [InlineData("schema_name()", "schema_name", 0)]
    [InlineData("col_length('t', 'c')", "col_length", 2)]
    [InlineData("newid()", "newid", 0)]
    [InlineData("newsequentialid()", "newsequentialid", 0)]
    [InlineData("session_context(N'k')", "session_context", 1)]
    [InlineData("context_info()", "context_info", 0)]
    [InlineData("user_name()", "user_name", 0)]
    [InlineData("suser_sname()", "suser_sname", 0)]
    [InlineData("original_login()", "original_login", 0)]
    public void Parses_aggregate_and_system_functions_as_calls(string expression, string name, int arguments)
    {
        Call(expression, name, arguments);
    }

    [Theory]
    [InlineData("dateadd(day, 1, a)", "day")]
    [InlineData("dateadd(yy, 1, a)", "yy")]
    [InlineData("datediff(mm, a, b)", "mm")]
    [InlineData("datediff_big(ms, a, b)", "ms")]
    [InlineData("datepart(dw, a)", "dw")]
    [InlineData("datepart(iso_week, a)", "iso_week")]
    [InlineData("datepart(tzoffset, a)", "tzoffset")]
    [InlineData("datename(month, a)", "month")]
    [InlineData("datepart(year, a)", "year")]
    [InlineData("datepart(second, a)", "second")]
    [InlineData("datetrunc(quarter, a)", "quarter")]
    [InlineData("date_bucket(week, 1, a)", "week")]
    public void Bare_datepart_keywords_are_identifier_arguments(string expression, string datepart)
    {
        var call = SqlAssert.ExprTsql<FunctionCallExpression>(expression);
        var part = Assert.IsType<IdentifierExpression>(call.Arguments[0]);
        Assert.Equal(datepart, ExprText(expression, part.Span));
    }

    [Fact]
    public void Current_timestamp_needs_no_parentheses()
    {
        var niladic = SqlAssert.ExprTsql<NiladicFunctionExpression>("current_timestamp");
        Assert.Equal(SyntaxKind.CurrentTimestampKeyword, niladic.Name.Kind);
        Assert.Null(niladic.OpenParen);
    }

    // ---- Keyword-named functions with plain arguments ----

    [Theory]
    [InlineData("left(a, 2)", SyntaxKind.LeftKeyword, 2)]
    [InlineData("right(a, 2)", SyntaxKind.RightKeyword, 2)]
    [InlineData("substring(a, 1, 2)", SyntaxKind.SubstringKeyword, 3)]
    [InlineData("translate(a, 'abc', 'xyz')", SyntaxKind.TranslateKeyword, 3)]
    public void Keyword_named_functions_take_plain_arguments(string expression, SyntaxKind kind, int arguments)
    {
        var call = SqlAssert.ExprTsql<FunctionCallExpression>(expression);
        Assert.Equal(kind, call.Name.Kind);
        Assert.Equal(arguments, call.Arguments.Count);
        SqlAssert.ParseFails($"{SelectPrefix}{expression}");
    }

    [Fact]
    public void Iso_substring_and_translate_forms_need_their_keywords_without_sql_server()
    {
        SqlAssert.Expr<SubstringExpression>("substring(a from 1 for 2)");
        SqlAssert.Expr<UsingTransformExpression>("translate(a using t)");
        SqlAssert.Expr<UsingTransformExpression>("convert(a using t)");
    }

    [Fact]
    public void Trim_keeps_its_iso_forms()
    {
        var both = SqlAssert.ExprTsql<TrimExpression>("trim(both 'x' from a)");
        Assert.NotNull(both.Specification);
        Assert.NotNull(both.Characters);
        var characters = SqlAssert.ExprTsql<TrimExpression>("trim('x' from a)");
        Assert.Null(characters.Specification);
        Assert.NotNull(characters.Characters);
        Assert.Null(SqlAssert.ExprTsql<TrimExpression>("trim(a)").Characters);
    }

    // ---- Conversions ----

    [Fact]
    public void Convert_takes_the_type_first_and_an_optional_style()
    {
        const string expression = "convert(varchar(10), a + 1, 120)";
        var convert = SqlAssert.ExprTsql<TsqlConvertExpression>(expression);
        Assert.Equal(SyntaxKind.ConvertKeyword, convert.FunctionName.Kind);
        Assert.Equal("varchar(10)", ExprText(expression, convert.Type.Span));
        Assert.IsType<BinaryExpression>(convert.Expression);
        Assert.Equal("120", ExprText(expression, Assert.IsType<LiteralExpression>(convert.Style).Span));
        Assert.Equal(expression, ExprText(expression, convert.Span));

        var plain = SqlAssert.ExprTsql<TsqlConvertExpression>("convert(int, a)");
        Assert.Null(plain.StyleComma);
        Assert.Null(plain.Style);
        SqlAssert.ParseFails("select convert(int, a)");
    }

    [Fact]
    public void Try_convert_is_a_convert()
    {
        const string expression = "try_convert(datetime2, a, 126)";
        var convert = SqlAssert.ExprTsql<TsqlConvertExpression>(expression);
        Assert.Equal("try_convert", ExprText(expression, convert.FunctionName.Span));
        Assert.NotNull(convert.Style);
        SqlAssert.Expr<FunctionCallExpression>(expression);
    }

    [Theory]
    [InlineData("try_cast(a as int)", "try_cast", false)]
    [InlineData("parse(a as datetime2 using 'en-US')", "parse", true)]
    [InlineData("try_parse(a as money using @culture)", "try_parse", true)]
    [InlineData("parse(a as int)", "parse", false)]
    public void Parses_cast_style_conversions(string expression, string name, bool hasCulture)
    {
        var cast = SqlAssert.ExprTsql<TsqlCastExpression>(expression);
        Assert.Equal(name, ExprText(expression, cast.FunctionName.Span));
        Assert.IsType<IdentifierExpression>(cast.Expression);
        Assert.Equal(hasCulture, cast.UsingKeyword is not null);
        Assert.Equal(hasCulture, cast.Culture is not null);
        Assert.Equal(expression, ExprText(expression, cast.Span));
        SqlAssert.ParseFails($"{SelectPrefix}{expression}");
    }

    [Fact]
    public void Try_cast_takes_no_culture()
    {
        SqlAssert.ParseFails("select try_cast(a as int using 'en-US')", SqlOptions.SqlServer);
    }

    // ---- AT TIME ZONE ----

    [Fact]
    public void At_time_zone_converts_a_datetime()
    {
        var atTimeZone = SqlAssert.ExprTsql<TsqlAtTimeZoneExpression>(AtTimeZone);
        Assert.Equal("d", ExprText(AtTimeZone, atTimeZone.Expression.Span));
        Assert.Equal("'UTC'", ExprText(AtTimeZone, atTimeZone.TimeZone.Span));
        Assert.Equal(AtTimeZone, ExprText(AtTimeZone, atTimeZone.Span));
        SqlAssert.ParseFails($"{SelectPrefix}{AtTimeZone}");
    }

    [Fact]
    public void At_time_zone_chains_and_binds_tighter_than_comparison()
    {
        const string chained = "d at time zone 'UTC' at time zone @zone";
        var outer = SqlAssert.ExprTsql<TsqlAtTimeZoneExpression>(chained);
        Assert.IsType<TsqlAtTimeZoneExpression>(outer.Expression);
        Assert.IsType<EmbeddedHostExpression>(outer.TimeZone);

        var comparison = Assert.IsType<BinaryExpression>(WhereOf("select a from t where d at time zone 'UTC' > b"));
        Assert.IsType<TsqlAtTimeZoneExpression>(comparison.Left);
    }

    [Fact]
    public void At_is_still_an_alias_when_no_time_zone_follows()
    {
        var item = Assert.Single(SqlAssert.SelectTsql("select a at from t").SelectList);
        Assert.IsType<IdentifierExpression>(item.Expression);
        Assert.NotNull(item.Alias);
    }

    // ---- Aggregates ----

    [Fact]
    public void String_agg_takes_within_group()
    {
        var aggregate = SqlAssert.ExprTsql<AggregateCallExpression>(StringAgg);
        Assert.Equal(Pair, aggregate.Arguments.Count);
        var withinGroup = Assert.IsType<WithinGroupClause>(aggregate.WithinGroup);
        Assert.Single(withinGroup.OrderBy.Items);
        Assert.Null(aggregate.Over);
        Assert.Equal(StringAgg, ExprText(StringAgg, aggregate.Span));
        Assert.IsType<FunctionCallExpression>(SqlAssert.ExprTsql("string_agg(a, ',')"));
        SqlAssert.ParseFails($"{SelectPrefix}{StringAgg}");
    }

    [Theory]
    [InlineData("percentile_cont(0.5) within group (order by a) over (partition by b)")]
    [InlineData("percentile_disc(0.5) within group (order by a desc) over ()")]
    [InlineData("approx_percentile_cont(0.5) within group (order by a)")]
    [InlineData("approx_percentile_disc(0.9) within group (order by a)")]
    public void Ordered_set_functions_take_within_group_and_over(string expression)
    {
        var aggregate = SqlAssert.ExprTsql<AggregateCallExpression>(expression);
        Assert.NotNull(aggregate.WithinGroup);
        Assert.Equal(expression.Contains(" over ", StringComparison.Ordinal), aggregate.Over is not null);
        Assert.Equal(expression, ExprText(expression, aggregate.Span));
    }

    [Theory]
    [InlineData("count(distinct a)")]
    [InlineData("count_big(distinct a)")]
    [InlineData("sum(all a)")]
    [InlineData("approx_count_distinct(distinct a)")]
    public void Aggregates_take_a_set_quantifier(string expression)
    {
        var aggregate = SqlAssert.ExprTsql<AggregateCallExpression>(expression);
        Assert.NotNull(aggregate.SetQuantifier);
        Assert.Single(aggregate.Arguments);
        SqlAssert.ParseFails($"{SelectPrefix}{expression}");
    }

    [Fact]
    public void Window_functions_keep_null_treatment_and_over()
    {
        var call = SqlAssert.ExprTsql<FunctionCallExpression>("first_value(a) ignore nulls over (order by b)");
        Assert.NotNull(call.NullTreatment);
        Assert.NotNull(call.Over);
        Assert.NotNull(SqlAssert.ExprTsql<FunctionCallExpression>("row_number() over (order by a)").Over);
    }

    // ---- JSON ----

    [Fact]
    public void Json_object_takes_colon_pairs_and_null_clauses()
    {
        const string expression = "json_object('a': 1, @k: @v, 'c': b absent on null)";
        var call = SqlAssert.ExprTsql<MarkupCallExpression>(expression);
        Assert.Equal(6, call.Arguments.Count);
        Assert.IsType<EmbeddedHostExpression>(call.Arguments[Pair]);
        Assert.Equal(SyntaxKind.ColonToken, call.Clauses[0].Kind);
        Assert.Equal(expression, ExprText(expression, call.Span));
        Assert.Empty(SqlAssert.ExprTsql<MarkupCallExpression>("json_object()").Arguments);
        SqlAssert.ParseFails($"{SelectPrefix}{expression}");
    }

    [Theory]
    [InlineData("json_array(1, a, null null on null)", 3)]
    [InlineData("json_objectagg(a : b)", 2)]
    [InlineData("json_arrayagg(a order by a)", 1)]
    [InlineData("json_value(a, '$.x')", 2)]
    [InlineData("json_query(a, '$.x')", 2)]
    public void Json_constructors_and_queries_are_markup_calls(string expression, int arguments)
    {
        var call = SqlAssert.ExprTsql<MarkupCallExpression>(expression);
        Assert.Equal(arguments, call.Arguments.Count);
    }

    // ---- XML and CLR methods ----

    [Theory]
    [InlineData("x.value('(/a)[1]', 'int')", "value", 2)]
    [InlineData("x.query('/a')", "query", 1)]
    [InlineData("t.x.nodes('/a')", "nodes", 1)]
    [InlineData("@x.exist('/a')", "exist", 1)]
    [InlineData("x.modify('delete /a')", "modify", 1)]
    [InlineData("geo.STDistance(@p)", "STDistance", 1)]
    [InlineData("node.GetAncestor(1)", "GetAncestor", 1)]
    [InlineData("@g.STBuffer(1).STArea()", "STArea", 0)]
    public void Instance_methods_are_method_invocations(string expression, string name, int arguments)
    {
        var call = SqlAssert.ExprTsql<MethodInvocationExpression>(expression);
        Assert.Equal(name, ExprText(expression, call.Name.Span));
        Assert.Equal(arguments, call.Arguments.Count);
        Assert.Equal(expression, ExprText(expression, call.Span));
    }

    [Theory]
    [InlineData("geography::Point(47.6, -122.3, 4326)", "Point", 3)]
    [InlineData("hierarchyid::GetRoot()", "GetRoot", 0)]
    [InlineData("geometry::STGeomFromText('POINT(1 2)', 0)", "STGeomFromText", 2)]
    [InlineData("dbo.Udt::Make(1, 2)", "Make", 2)]
    public void Static_methods_are_static_method_invocations(string expression, string name, int arguments)
    {
        var call = SqlAssert.ExprTsql<StaticMethodInvocationExpression>(expression);
        Assert.Equal(name, ExprText(expression, call.Name.Span));
        Assert.Equal(arguments, call.Arguments.Count);
        Assert.Equal(expression, ExprText(expression, call.Span));
    }

    [Fact]
    public void Double_colon_is_never_a_cast_under_sql_server()
    {
        SqlAssert.ParseFails("select a::int", SqlOptions.SqlServer);
        SqlAssert.Expr<ColonCastExpression>("a::Udt(1, 2)");
        Assert.IsType<MethodInvocationExpression>(SqlAssert.ExprTsql("hierarchyid::GetRoot().ToString()"));
    }

    [Theory]
    [InlineData("@p.X", "X")]
    [InlineData("@p.STSrid", "STSrid")]
    [InlineData("t.loc.Lat", "Lat")]
    public void Properties_are_member_accesses(string expression, string member)
    {
        var access = SqlAssert.ExprTsql<MemberAccessExpression>(expression);
        Assert.Equal(member, ExprText(expression, access.Member.Span));
    }

    // ---- AI functions ----

    [Fact]
    public void Generate_embeddings_names_its_model()
    {
        var embeddings = SqlAssert.ExprTsql<TsqlGenerateEmbeddingsExpression>(Embeddings);
        Assert.IsType<IdentifierExpression>(embeddings.Source);
        Assert.Equal(["dbo", "m"], embeddings.ModelName.Select(part => ExprText(Embeddings, part.Span)));
        Assert.NotNull(embeddings.ParametersKeyword);
        Assert.IsType<LiteralExpression>(embeddings.Parameters);
        Assert.Equal(Embeddings, ExprText(Embeddings, embeddings.Span));

        var plain = SqlAssert.ExprTsql<TsqlGenerateEmbeddingsExpression>("ai_generate_embeddings(N'text' use model m)");
        Assert.Null(plain.Parameters);
        SqlAssert.ParseFails($"{SelectPrefix}{Embeddings}");
        SqlAssert.ParseFails("select ai_generate_embeddings(a model m)", SqlOptions.SqlServer);
    }

    [Fact]
    public void Generate_chunks_takes_named_arguments()
    {
        const string expression = "ai_generate_chunks(source = a, chunk_type = fixed, chunk_size = 10)";
        var call = SqlAssert.ExprTsql<FunctionCallExpression>(expression);
        var names = call.Arguments.Select(argument => ExprText(expression, Assert.IsType<NamedArgumentExpression>(argument).Name.Span));
        Assert.Equal(["source", "chunk_type", "chunk_size"], names);
        Assert.IsType<BinaryExpression>(SqlAssert.Expr<FunctionCallExpression>(expression).Arguments[0]);
    }

    // ---- Sequences ----

    [Fact]
    public void Next_value_for_takes_an_over_clause()
    {
        var over = SqlAssert.ExprTsql<TsqlNextValueOverExpression>(NextValueOver);
        Assert.Equal(Pair, over.NextValue.NameParts.Count);
        Assert.NotNull(over.Over.OrderBy);
        Assert.Equal(NextValueOver, ExprText(NextValueOver, over.Span));
        SqlAssert.ExprTsql<NextValueExpression>("next value for s");
        SqlAssert.ParseFails($"{SelectPrefix}{NextValueOver}");
    }

    // ---- Full-text predicates ----

    [Fact]
    public void Contains_takes_a_column_list_and_language()
    {
        var predicate = Assert.IsType<TsqlFullTextPredicateExpression>(WhereOf(ContainsColumns));
        Assert.Equal(Pair, Assert.IsType<RowConstructorExpression>(predicate.Columns).Elements.Count);
        Assert.IsType<LiteralExpression>(predicate.SearchCondition);
        Assert.NotNull(predicate.LanguageKeyword);
        Assert.IsType<LiteralExpression>(predicate.Language);
        SqlAssert.ParseFails(ContainsColumns);
    }

    [Fact]
    public void Freetext_searches_every_column()
    {
        var predicate = Assert.IsType<TsqlFullTextPredicateExpression>(WhereOf(FreetextStar));
        Assert.IsType<StarExpression>(predicate.Columns);
        Assert.Null(predicate.Language);
        SqlAssert.ParseFails(FreetextStar);
    }

    [Fact]
    public void Full_text_predicates_combine_with_other_conditions()
    {
        const string sql = "select a from t where contains(property(b, 'Title'), '\"x*\"') and freetext(c, @q, language N'English')";
        var and = Assert.IsType<BinaryExpression>(WhereOf(sql));
        var contains = Assert.IsType<TsqlFullTextPredicateExpression>(and.Left);
        Assert.IsType<FunctionCallExpression>(contains.Columns);
        var freetext = Assert.IsType<TsqlFullTextPredicateExpression>(and.Right);
        Assert.IsType<EmbeddedHostExpression>(freetext.SearchCondition);
        SqlAssert.ParseFails("select a from t where contains(b, 'x', 1033)", SqlOptions.SqlServer);
    }

    // ---- Visitor ----

    private sealed class TypeRecorder : SqlVisitor
    {
        public HashSet<Type> Types { get; } = [];

        public override void Visit(Expression expression)
        {
            Types.Add(expression.GetType());
            base.Visit(expression);
        }
    }

    [Fact]
    public void Visitor_walks_t_sql_expressions()
    {
        const string sql = "select convert(int, a, 1), try_cast(b as xml(dbo.sc)), parse(c as int using 'en-US'), "
            + "d at time zone 'UTC', next value for s over (order by a), ai_generate_embeddings(e use model m parameters '{}'), "
            + "string_agg(f, ',') within group (order by f) from t where contains(*, 'x', language 1033)";
        var visitor = new TypeRecorder();
        visitor.Visit(SqlAssert.SelectTsql(sql));
        Assert.Contains(typeof(TsqlConvertExpression), visitor.Types);
        Assert.Contains(typeof(TsqlCastExpression), visitor.Types);
        Assert.Contains(typeof(TsqlAtTimeZoneExpression), visitor.Types);
        Assert.Contains(typeof(TsqlNextValueOverExpression), visitor.Types);
        Assert.Contains(typeof(TsqlGenerateEmbeddingsExpression), visitor.Types);
        Assert.Contains(typeof(TsqlFullTextPredicateExpression), visitor.Types);
        Assert.Contains(typeof(AggregateCallExpression), visitor.Types);
        Assert.Contains(typeof(StarExpression), visitor.Types);
    }
}
