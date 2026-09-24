namespace QlParse.Tests;

public sealed class ParsePrefixTests
{
    private sealed class EmptyVisitor : SqlVisitor;

    [Theory]
    [InlineData("1", SyntaxKind.Number)]
    [InlineData("1e10", SyntaxKind.Number)]
    [InlineData("'x'", SyntaxKind.String)]
    [InlineData("U&'foo'", SyntaxKind.String)]
    [InlineData("_utf8'hi'", SyntaxKind.String)]
    [InlineData("true", SyntaxKind.TrueKeyword)]
    [InlineData("false", SyntaxKind.FalseKeyword)]
    [InlineData("null", SyntaxKind.NullKeyword)]
    public void Parses_literals(string sql, SyntaxKind kind)
    {
        Assert.Equal(kind, SqlAssert.Expr<LiteralExpression>(sql).Literal.Kind);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("U&\"foo\"")]
    [InlineData("_latin1\"foo\"")]
    public void Parses_identifier(string sql)
    {
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr(sql));
    }

    [Theory]
    [InlineData("?")]
    public void Parses_host_parameter(string sql)
    {
        Assert.IsType<HostParameterExpression>(SqlAssert.Expr(sql));
    }

    [Theory]
    [InlineData(":foo")]
    public void Parses_embedded_host(string sql)
    {
        Assert.IsType<EmbeddedHostExpression>(SqlAssert.Expr(sql));
    }

    [Fact]
    public void Parses_comparison_with_embedded_host()
    {
        var eq = SqlAssert.Expr<BinaryExpression>("x = :id");
        Assert.Equal(SyntaxKind.EqualsToken, eq.OperatorToken.Kind);
        Assert.IsType<EmbeddedHostExpression>(eq.Right);
    }

    [Fact]
    public void Parses_at_parameter_with_flag()
    {
        var result = Sql.Parse("select x = @auditEventId", SqlFlags.AtParameters);
        Assert.Null(result.Error);
        var eq = Assert.IsType<BinaryExpression>(Assert.IsType<SelectStatement>(result.Root).SelectList[0].Expression);
        Assert.IsType<EmbeddedHostExpression>(eq.Right);
    }

    [Fact]
    public void Parses_comparison_with_host_parameter()
    {
        var eq = SqlAssert.Expr<BinaryExpression>("x = ?");
        Assert.Equal(SyntaxKind.EqualsToken, eq.OperatorToken.Kind);
        Assert.IsType<HostParameterExpression>(eq.Right);
    }

    [Fact]
    public void Parses_not()
    {
        Assert.IsType<NotExpression>(SqlAssert.Expr("not x"));
    }

    [Theory]
    [InlineData("-x", SyntaxKind.MinusToken)]
    [InlineData("+x", SyntaxKind.PlusToken)]
    public void Parses_unary(string sql, SyntaxKind kind)
    {
        Assert.Equal(kind, SqlAssert.Expr<UnaryExpression>(sql).OperatorToken.Kind);
    }

    [Fact]
    public void Parses_searched_and_simple_case()
    {
        var searched = SqlAssert.Expr<CaseExpression>("case when 1 then 2 when 3 then 4 else 5 end");
        Assert.Null(searched.Operand);
        Assert.Equal(Count.Two, searched.Arms.Count);
        Assert.NotNull(searched.ElseKeyword);

        var simple = SqlAssert.Expr<CaseExpression>("case x when 1 then 2 end");
        Assert.NotNull(simple.Operand);
        Assert.Null(simple.ElseKeyword);
    }

    [Fact]
    public void Case_without_when_throws()
    {
        Assert.NotNull(Sql.Parse("select case x end").Error);
    }

    [Fact]
    public void Parses_cast_and_types()
    {
        var cast = SqlAssert.Expr<CastExpression>("cast(x as int)");
        Assert.Equal(SyntaxKind.Identifier, cast.Type.Name.Kind);
        Assert.Empty(cast.Type.NameTail);

        var dbl = SqlAssert.Expr<CastExpression>("cast(x as double precision)").Type;
        Assert.Single(dbl.NameTail);

        var varying = SqlAssert.Expr<CastExpression>("cast(x as char varying(10))").Type;
        Assert.Single(varying.NameTail);
        Assert.NotNull(varying.Precision);

        var numeric = SqlAssert.Expr<CastExpression>("cast(x as numeric(10, 2))").Type;
        Assert.NotNull(numeric.Scale);

        var tstz = SqlAssert.Expr<CastExpression>("cast(x as timestamp(6) with time zone)").Type;
        Assert.NotNull(tstz.WithKeyword);
        Assert.NotNull(tstz.Zone);

        var clob = SqlAssert.Expr<CastExpression>("cast(x as character large object)");
        Assert.Equal(Count.Two, clob.Type.NameTail.Count);

        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as char(10))").Type.Precision);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as varchar(10))").Type.Precision);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as clob)").Type.NameTail);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as char large object(10))").Type.Precision);
    }

    [Fact]
    public void Parses_treat()
    {
        Assert.IsType<TreatExpression>(SqlAssert.Expr("treat(x as int)"));
        Assert.Single(SqlAssert.Expr<TreatExpression>("treat(x as public.my_udt)").Type.NameTail);
    }

    [Fact]
    public void Treat_as_column_throws()
    {
        Assert.NotNull(Sql.Parse("select treat from t").Error);
    }

    [Fact]
    public void Parses_ref_values_and_deref()
    {
        var deref = SqlAssert.Expr<DerefExpression>("deref( p.manager )");
        Assert.IsType<MemberAccessExpression>(deref.Expression);
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr<RefValueExpression>("ref( p )").Expression);
        Assert.NotNull(Sql.Parse("select deref from t").Error);
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr("ref "));
        new EmptyVisitor().Visit(deref);
    }

    [Fact]
    public void Parses_specifictype()
    {
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr<SpecifictypeExpression>("specifictype( p )").Expression);
        Assert.NotNull(Sql.Parse("select specifictype from t").Error);
    }

    [Fact]
    public void Parses_new_specification()
    {
        var created = SqlAssert.Expr<NewSpecificationExpression>("new sch.person(p.first_name)");
        Assert.Equal(Count.Two, created.TypeName.Count);
        Assert.Single(created.Arguments);
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr("new "));
        new EmptyVisitor().Visit(created);
    }

    [Fact]
    public void Parses_nullif()
    {
        Assert.IsType<NullIfExpression>(SqlAssert.Expr("nullif(a, b)"));
    }

    [Fact]
    public void Nullif_arity_and_keyword_throw()
    {
        Assert.NotNull(Sql.Parse("select nullif(a)").Error);
        Assert.NotNull(Sql.Parse("select nullif from t").Error);
    }

    [Fact]
    public void Parses_coalesce()
    {
        Assert.Equal(Count.Two, SqlAssert.Expr<CoalesceExpression>("coalesce(a, b)").Arguments.Count);
        Assert.Equal(Count.Three, SqlAssert.Expr<CoalesceExpression>("coalesce(a, b, c)").Arguments.Count);
    }

    [Fact]
    public void Coalesce_arity_and_keyword_throw()
    {
        Assert.NotNull(Sql.Parse("select coalesce(a)").Error);
        Assert.NotNull(Sql.Parse("select coalesce from t").Error);
    }

    [Fact]
    public void Parses_next_value_for()
    {
        Assert.Single(SqlAssert.Expr<NextValueExpression>("next value for seq").NameParts);
        Assert.Equal(Count.Two, SqlAssert.Expr<NextValueExpression>("next value for public.seq").NameParts.Count);
    }

    [Fact]
    public void Next_alone_is_identifier()
    {
        Assert.IsType<IdentifierExpression>(SqlAssert.Select("select next from t").SelectList[0].Expression);
    }

    [Fact]
    public void Parses_exact_numeric_types()
    {
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as numeric)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as decimal)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as smallint)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as integer)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as bigint)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as int)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as dec)").Type.NameTail);
    }

    [Fact]
    public void Parses_approximate_numeric_types()
    {
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as float)").Type.NameTail);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as float(53))").Type.Precision);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as real)").Type.NameTail);
    }

    [Fact]
    public void Parses_national_character_types()
    {
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as nchar(10))").Type.Precision);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as nvarchar(10))").Type.Precision);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as nclob)").Type.NameTail);
        var national = SqlAssert.Expr<CastExpression>("cast(x as national character varying(10))").Type;
        Assert.Equal(Count.Two, national.NameTail.Count);
        Assert.NotNull(national.Precision);
    }

    [Fact]
    public void Parses_binary_types()
    {
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as binary(8))").Type.Precision);
        Assert.NotNull(SqlAssert.Expr<CastExpression>("cast(x as varbinary(8))").Type.Precision);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as blob)").Type.NameTail);
        Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as binary varying(8))").Type.NameTail);
        Assert.Equal(Count.Two, SqlAssert.Expr<CastExpression>("cast(x as binary large object)").Type.NameTail.Count);
    }

    [Fact]
    public void Parses_xml_and_json_types()
    {
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as xml)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as json)").Type.NameTail);
        Assert.Empty(SqlAssert.Expr<ColonCastExpression>("x :: xml").Type.NameTail);
    }

    [Fact]
    public void Parses_array_types()
    {
        var arrayType = SqlAssert.Expr<CastExpression>("cast(x as integer array)").Type;
        var suffix = Assert.Single(arrayType.Collections);
        Assert.Equal(SyntaxKind.ArrayKeyword, suffix.Keyword.Kind);
        Assert.Null(suffix.OpenBracket);

        var bounded = SqlAssert.Expr<CastExpression>("cast(x as integer array[3])").Type;
        Assert.NotNull(Assert.Single(bounded.Collections).Cardinality);

        Assert.IsType<ArrayExpression>(SqlAssert.Expr<CastExpression>("cast(array[1, 2] as integer array)").Expression);
        Assert.IsType<ArrayExpression>(SqlAssert.Expr<ColonCastExpression>("array[1] :: integer array").Expression);
    }

    [Fact]
    public void Parses_multiset_types()
    {
        var multiset = Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as integer multiset)").Type.Collections);
        Assert.Equal(SyntaxKind.MultisetKeyword, multiset.Keyword.Kind);

        Assert.Equal(Count.Two, SqlAssert.Expr<CastExpression>("cast(x as integer array multiset)").Type.Collections.Count);
    }

    [Fact]
    public void Parses_row_types()
    {
        var row = SqlAssert.Expr<CastExpression>("cast(x as row(a int, b varchar(10)))").Type;
        Assert.Equal(Count.Two, row.Fields!.Count);
        Assert.NotNull(row.Fields[1].Type.Precision);

        Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as row(a int) array)").Type.Collections);
    }

    [Fact]
    public void Parses_ref_types()
    {
        var refType = SqlAssert.Expr<CastExpression>("cast(x as ref(foo))").Type;
        Assert.NotNull(refType.ReferencedType);

        var scoped = SqlAssert.Expr<CastExpression>("cast(x as ref(foo) scope t)").Type;
        Assert.NotNull(scoped.ScopeKeyword);
        Assert.NotNull(scoped.ScopeName);
    }

    [Fact]
    public void Parses_user_defined_types()
    {
        Assert.Empty(SqlAssert.Expr<CastExpression>("cast(x as my_udt)").Type.NameTail);
        Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as public.my_udt)").Type.NameTail);
        Assert.Equal(Count.Two, SqlAssert.Expr<CastExpression>("cast(x as catalog.schema.my_udt)").Type.NameTail.Count);
    }

    [Fact]
    public void Parses_mdarray_types()
    {
        var simple = Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as integer mdarray[5])").Type.Collections);
        Assert.Single(simple.Dimensions!);

        var ranged = Assert.Single(SqlAssert.Expr<CastExpression>("cast(x as integer mdarray[0:4, 0:9])").Type.Collections);
        Assert.Equal(Count.Two, ranged.Dimensions!.Count);
        Assert.NotNull(ranged.Dimensions[0].Colon);
    }

    [Fact]
    public void Row_type_without_field_name_throws()
    {
        Assert.NotNull(Sql.Parse("select cast(x as row(1))").Error);
    }

    [Theory]
    [InlineData("array[]")]
    public void Parses_empty_array(string sql)
    {
        Assert.Empty(SqlAssert.Expr<ArrayExpression>(sql).Elements);
    }

    [Fact]
    public void Parses_array_elements()
    {
        Assert.Equal(Count.Two, SqlAssert.Expr<ArrayExpression>("array[1, 2]").Elements.Count);
    }

    [Fact]
    public void Parses_array_subquery()
    {
        var query = SqlAssert.Expr<ArrayQueryExpression>("array(select a from t order by a)");
        Assert.NotNull(Assert.IsType<SelectStatement>(query.Query).OrderBy);
    }

    [Fact]
    public void Parses_multiset_constructors()
    {
        Assert.Empty(SqlAssert.Expr<MultisetExpression>("multiset[]").Elements);
        Assert.Equal(Count.Two, SqlAssert.Expr<MultisetExpression>("multiset[1, 2]").Elements.Count);
        Assert.Equal(SyntaxKind.MultisetKeyword, SqlAssert.Expr<MultisetQueryExpression>("multiset(select 1)").Keyword.Kind);
        Assert.Equal(SyntaxKind.Identifier, SqlAssert.Expr<MultisetQueryExpression>("table(select 1)").Keyword.Kind);
        Assert.IsType<MultisetExpression>(SqlAssert.Expr<MultisetSetExpression>("set(multiset[1])").Expression);
    }

    [Fact]
    public void Parses_cardinality_element_and_absent()
    {
        Assert.Equal(SyntaxKind.CardinalityKeyword, SqlAssert.Expr<SpecialFormExpression>("cardinality(array[1])").Name.Kind);
        Assert.Equal(SyntaxKind.ElementKeyword, SqlAssert.Expr<SpecialFormExpression>("element(multiset[1])").Name.Kind);
        Assert.Equal(SyntaxKind.NullKeyword, SqlAssert.Expr<AbsentOnNullExpression>("absent on null").NullKeyword.Kind);
    }

    [Theory]
    [InlineData("date '2020-01-01'", SyntaxKind.DateKeyword)]
    [InlineData("time '12:00:00'", SyntaxKind.TimeKeyword)]
    [InlineData("timestamp '2020-01-01'", SyntaxKind.TimestampKeyword)]
    public void Parses_datetime_literals(string sql, SyntaxKind kind)
    {
        Assert.Equal(kind, SqlAssert.Expr<DatetimeLiteralExpression>(sql).KindKeyword.Kind);
    }

    [Fact]
    public void Parses_interval_literal()
    {
        var basic = SqlAssert.Expr<IntervalLiteralExpression>("interval '1' day");
        Assert.Null(basic.Sign);
        Assert.Null(basic.Qualifier.ToKeyword);

        var signed = SqlAssert.Expr<IntervalLiteralExpression>("interval - '1' day to hour");
        Assert.Equal(SyntaxKind.MinusToken, signed.Sign!.Value.Kind);
        Assert.NotNull(signed.Qualifier.End);

        var precision = SqlAssert.Expr<IntervalLiteralExpression>("interval '1' second(3, 2)");
        Assert.NotNull(precision.Qualifier.Start.Precision);
        Assert.NotNull(precision.Qualifier.Start.Scale);
    }

    [Theory]
    [InlineData("trim(x)")]
    public void Parses_trim_source_only(string sql)
    {
        Assert.Null(SqlAssert.Expr<TrimExpression>(sql).FromKeyword);
    }

    [Fact]
    public void Parses_trim()
    {
        Assert.NotNull(SqlAssert.Expr<TrimExpression>("trim(both from x)").Specification);
        Assert.NotNull(SqlAssert.Expr<TrimExpression>("trim(leading ' ' from x)").Characters);
        Assert.NotNull(SqlAssert.Expr<TrimExpression>("trim(' ' from x)").Characters);
    }

    [Theory]
    [InlineData("upper(x)", SyntaxKind.UpperKeyword)]
    [InlineData("lower(x)", SyntaxKind.LowerKeyword)]
    [InlineData("char_length(x)", SyntaxKind.CharLengthKeyword)]
    [InlineData("octet_length(x)", SyntaxKind.OctetLengthKeyword)]
    [InlineData("bit_length(x)", SyntaxKind.BitLengthKeyword)]
    [InlineData("normalize(x)", SyntaxKind.NormalizeKeyword)]
    [InlineData("floor(x)", SyntaxKind.FloorKeyword)]
    [InlineData("ceil(x)", SyntaxKind.CeilKeyword)]
    [InlineData("sqrt(x)", SyntaxKind.SqrtKeyword)]
    [InlineData("ln(x)", SyntaxKind.LnKeyword)]
    [InlineData("exp(x)", SyntaxKind.ExpKeyword)]
    [InlineData("abs(x)", SyntaxKind.AbsKeyword)]
    public void Parses_unary_special_forms(string sql, SyntaxKind kind)
    {
        var form = SqlAssert.Expr<SpecialFormExpression>(sql);
        Assert.Equal(kind, form.Name.Kind);
        Assert.Single(form.Arguments);
    }

    [Fact]
    public void Parses_special_forms_with_extra_args()
    {
        Assert.Equal(Count.Two, SqlAssert.Expr<SpecialFormExpression>("power(x, 2)").Arguments.Count);
        Assert.Equal(Count.Two, SqlAssert.Expr<SpecialFormExpression>("mod(x, 2)").Arguments.Count);
        Assert.Equal(Count.Two, SqlAssert.Expr<SpecialFormExpression>("normalize(x, nfc)").Arguments.Count);
        Assert.Equal(Count.Three, SqlAssert.Expr<SpecialFormExpression>("width_bucket(x, 0, 1)").Arguments.Count);
        Assert.NotNull(SqlAssert.Expr<SpecialFormExpression>("char_length(x using characters)").UsingKeyword);
    }

    [Fact]
    public void Parses_overlay()
    {
        Assert.Null(SqlAssert.Expr<OverlayExpression>("overlay(x placing y from 1)").ForKeyword);
        Assert.NotNull(SqlAssert.Expr<OverlayExpression>("overlay(x placing y from 1 for 2)").Length);
    }

    [Fact]
    public void Parses_extract_substring_position()
    {
        Assert.IsType<ExtractExpression>(SqlAssert.Expr("extract(year from x)"));
        Assert.Null(SqlAssert.Expr<SubstringExpression>("substring(x from 1)").ForKeyword);
        Assert.NotNull(SqlAssert.Expr<SubstringExpression>("substring(x from 1 for 2)").Length);
        Assert.IsType<PositionExpression>(SqlAssert.Expr("position('a' in x)"));
    }

    [Theory]
    [InlineData("convert(x using utf8)")]
    [InlineData("translate(x using latin1)")]
    public void Parses_using_transform(string sql)
    {
        Assert.IsType<UsingTransformExpression>(SqlAssert.Expr(sql));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("current_date")]
    [InlineData("current_user")]
    [InlineData("session_user")]
    [InlineData("system_user")]
    [InlineData("current_time")]
    [InlineData("localtime")]
    [InlineData("localtimestamp")]
    [InlineData("current_role")]
    [InlineData("current_catalog")]
    [InlineData("current_schema")]
    [InlineData("current_path")]
    public void Parses_niladic_functions(string sql)
    {
        Assert.Null(SqlAssert.Expr<NiladicFunctionExpression>(sql).OpenParen);
    }

    [Theory]
    [InlineData("current_time(3)")]
    [InlineData("current_timestamp(6)")]
    [InlineData("localtime(3)")]
    [InlineData("localtimestamp(6)")]
    public void Parses_niladic_functions_with_precision(string sql)
    {
        Assert.NotNull(SqlAssert.Expr<NiladicFunctionExpression>(sql).Precision);
    }

    [Fact]
    public void Niladic_without_precision_rejects_parens()
    {
        Assert.NotNull(Sql.Parse("select user()").Error);
        Assert.NotNull(Sql.Parse("select current_date()").Error);
        Assert.NotNull(Sql.Parse("select current_role()").Error);
    }

    [Fact]
    public void Parses_exists_and_unique()
    {
        Assert.IsType<ExistsExpression>(SqlAssert.Expr("exists (select 1)"));
        Assert.IsType<UniqueExpression>(SqlAssert.Expr("unique (select 1)"));
    }

    [Theory]
    [InlineData("(1)")]
    public void Parses_paren_expression(string sql)
    {
        Assert.IsType<ParenExpression>(SqlAssert.Expr(sql));
    }

    [Fact]
    public void Parses_subquery_and_row()
    {
        Assert.IsType<ScalarSubqueryExpression>(SqlAssert.Expr("(select 1)"));
        Assert.Equal(Count.Two, SqlAssert.Expr<RowConstructorExpression>("(1, 2)").Elements.Count);
    }

    [Theory]
    [InlineData("foo()")]
    public void Parses_empty_function_call(string sql)
    {
        Assert.Empty(SqlAssert.Expr<FunctionCallExpression>(sql).Arguments);
    }

    [Fact]
    public void Parses_function_call()
    {
        Assert.Equal(Count.Two, SqlAssert.Expr<FunctionCallExpression>("foo(1, 2)").Arguments.Count);
        Assert.IsType<StarExpression>(SqlAssert.Expr<FunctionCallExpression>("count(*)").Arguments[0]);
        Assert.NotNull(SqlAssert.Expr<FunctionCallExpression>("count(*) filter (where x > 1)").Filter);
    }

    [Fact]
    public void Unknown_prefix_throws()
    {
        Assert.NotNull(Sql.Parse("select )").Error);
    }
}
