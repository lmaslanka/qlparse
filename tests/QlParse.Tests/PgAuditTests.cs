namespace QlParse.Tests;

// Postgres roadmap items that the core grammar already covers; each test pins the AST shape
// produced under SqlOptions.Postgres so a dialect change can't silently regress them.
public sealed class PgAuditTests
{
    private const int SelectPrefixLength = 7;
    private const int TwoItems = 2;

    [Fact]
    public void Parses_colon_cast()
    {
        var cast = SqlAssert.ExprPg<ColonCastExpression>("a::int");
        Assert.Equal(SyntaxKind.DoubleColonToken, cast.DoubleColon.Kind);
        Assert.Equal(new SourceSpan(SelectPrefixLength, 6), cast.Span);
    }

    [Fact]
    public void Parses_object_identifier_cast_shorthand()
    {
        var cast = SqlAssert.ExprPg<ColonCastExpression>("'t'::regclass");
        Assert.IsType<LiteralExpression>(cast.Expression);
        Assert.Equal(new SourceSpan(SelectPrefixLength, 13), cast.Span);
    }

    [Theory]
    [InlineData("array_length")]
    [InlineData("array_upper")]
    [InlineData("array_lower")]
    [InlineData("array_ndims")]
    [InlineData("array_append")]
    [InlineData("array_prepend")]
    [InlineData("array_cat")]
    [InlineData("array_remove")]
    [InlineData("array_replace")]
    [InlineData("array_to_string")]
    [InlineData("string_to_array")]
    [InlineData("array_position")]
    [InlineData("array_positions")]
    [InlineData("generate_subscripts")]
    [InlineData("array_agg")]
    [InlineData("generate_series")]
    public void Parses_array_and_set_returning_functions(string name)
    {
        AssertFunctionCall(name);
    }

    [Theory]
    [InlineData("jsonb_build_object")]
    [InlineData("jsonb_build_array")]
    [InlineData("json_build_object")]
    [InlineData("json_build_array")]
    [InlineData("json_agg")]
    [InlineData("jsonb_agg")]
    [InlineData("json_object_agg")]
    [InlineData("jsonb_object_agg")]
    [InlineData("jsonb_set")]
    [InlineData("jsonb_set_lax")]
    [InlineData("jsonb_insert")]
    [InlineData("jsonb_strip_nulls")]
    [InlineData("json_strip_nulls")]
    [InlineData("jsonb_pretty")]
    [InlineData("jsonb_path_query")]
    [InlineData("jsonb_path_exists")]
    [InlineData("jsonb_path_match")]
    [InlineData("json_to_record")]
    [InlineData("jsonb_to_record")]
    [InlineData("json_to_recordset")]
    [InlineData("jsonb_to_recordset")]
    [InlineData("jsonb_populate_record")]
    [InlineData("jsonb_populate_recordset")]
    [InlineData("row_to_json")]
    [InlineData("to_json")]
    [InlineData("to_jsonb")]
    [InlineData("jsonb_each")]
    [InlineData("jsonb_each_text")]
    [InlineData("jsonb_array_elements")]
    [InlineData("jsonb_array_elements_text")]
    public void Parses_json_functions(string name)
    {
        AssertFunctionCall(name);
    }

    [Theory]
    [InlineData("format")]
    [InlineData("concat")]
    [InlineData("concat_ws")]
    [InlineData("split_part")]
    [InlineData("regexp_matches")]
    [InlineData("regexp_replace")]
    [InlineData("regexp_split_to_array")]
    [InlineData("regexp_split_to_table")]
    [InlineData("regexp_count")]
    [InlineData("to_char")]
    [InlineData("to_number")]
    [InlineData("to_date")]
    [InlineData("to_timestamp")]
    [InlineData("quote_ident")]
    [InlineData("quote_literal")]
    [InlineData("quote_nullable")]
    [InlineData("starts_with")]
    public void Parses_string_functions(string name)
    {
        AssertFunctionCall(name);
    }

    [Theory]
    [InlineData("now")]
    [InlineData("clock_timestamp")]
    [InlineData("statement_timestamp")]
    [InlineData("transaction_timestamp")]
    [InlineData("age")]
    [InlineData("date_trunc")]
    [InlineData("date_part")]
    [InlineData("make_date")]
    [InlineData("make_time")]
    [InlineData("make_timestamp")]
    [InlineData("make_timestamptz")]
    [InlineData("make_interval")]
    [InlineData("isfinite")]
    public void Parses_date_time_functions(string name)
    {
        AssertFunctionCall(name);
    }

    [Theory]
    [InlineData("pg_typeof")]
    [InlineData("current_setting")]
    [InlineData("set_config")]
    [InlineData("nextval")]
    [InlineData("currval")]
    [InlineData("setval")]
    [InlineData("lastval")]
    [InlineData("txid_current")]
    [InlineData("pg_current_xact_id")]
    [InlineData("to_tsvector")]
    [InlineData("to_tsquery")]
    [InlineData("plainto_tsquery")]
    [InlineData("phraseto_tsquery")]
    [InlineData("websearch_to_tsquery")]
    [InlineData("ts_rank")]
    [InlineData("ts_rank_cd")]
    [InlineData("ts_headline")]
    [InlineData("setweight")]
    public void Parses_system_and_text_search_functions(string name)
    {
        AssertFunctionCall(name);
    }

    [Fact]
    public void Parses_offset_without_fetch()
    {
        var select = SqlAssert.SelectPg("select a from t offset 3");
        Assert.NotNull(select.Offset);
        Assert.Null(select.Fetch);
    }

    [Fact]
    public void Parses_tablesample_with_repeatable()
    {
        var select = SqlAssert.SelectPg("select * from t tablesample bernoulli (10) repeatable (1)");
        var sampled = Assert.IsType<SampledTable>(select.From);
        Assert.NotNull(sampled.RepeatArgument);
    }

    [Fact]
    public void Parses_set_returning_function_in_from_without_lateral()
    {
        var select = SqlAssert.SelectPg("select * from t, generate_series(1, t.n) g");
        Assert.Single(select.ExtraFrom);
    }

    [Theory]
    [InlineData("select ctid, xmin, xmax, cmin, cmax, tableoid, oid from t")]
    [InlineData("select relname from pg_catalog.pg_class")]
    public void Parses_system_columns_and_catalog_names(string sql)
    {
        Assert.NotEmpty(SqlAssert.SelectPg(sql).SelectList);
    }

    [Fact]
    public void Parses_partial_index()
    {
        var index = SqlAssert.ParsePg<CreateIndexStatement>("create index i on t (a) where b > 0");
        Assert.IsType<BinaryExpression>(index.Where);
    }

    [Fact]
    public void Parses_expression_index()
    {
        var index = SqlAssert.ParsePg<CreateIndexStatement>("create index i on t ((lower(a)))");
        Assert.Single(index.Columns);
    }

    [Fact]
    public void Parses_create_sequence_as_type()
    {
        var sequence = SqlAssert.ParsePg<CreateSequenceStatement>("create sequence s as bigint");
        Assert.NotNull(sequence.DataType);
    }

    [Fact]
    public void Parses_create_composite_type()
    {
        var type = SqlAssert.ParsePg<CreateTypeStatement>("create type c as (a int, b text)");
        Assert.Equal(TwoItems, type.Attributes!.Count);
    }

    private static void AssertFunctionCall(string name)
    {
        var call = SqlAssert.ExprPg<FunctionCallExpression>($"{name}(a, b)");
        Assert.Equal(TwoItems, call.Arguments.Count);
    }
}
