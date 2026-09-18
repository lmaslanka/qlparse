namespace QlParse.Tests;

public sealed class ParsePrefixTests
{
    [Theory]
    [InlineData("1", SyntaxKind.Number)]
    [InlineData("'x'", SyntaxKind.String)]
    [InlineData("true", SyntaxKind.TrueKeyword)]
    [InlineData("false", SyntaxKind.FalseKeyword)]
    [InlineData("null", SyntaxKind.NullKeyword)]
    public void Parses_literals(string sql, SyntaxKind kind)
    {
        Assert.Equal(kind, SqlAssert.Expr<LiteralExpression>(sql).Literal.Kind);
    }

    [Theory]
    [InlineData("a")]
    public void Parses_identifier(string sql)
    {
        Assert.IsType<IdentifierExpression>(SqlAssert.Expr(sql));
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
        Assert.Throws<SqlParseException>(() => Sql.Parse("select case x end"));
    }

    [Fact]
    public void Parses_cast_and_types()
    {
        var cast = SqlAssert.Expr<CastExpression>("cast(x as int)");
        Assert.Equal(SyntaxKind.Identifier, cast.Type.Name.Kind);
        Assert.Null(cast.Type.SecondName);

        var dbl = SqlAssert.Expr<CastExpression>("cast(x as double precision)").Type;
        Assert.NotNull(dbl.SecondName);

        var varying = SqlAssert.Expr<CastExpression>("cast(x as char varying(10))").Type;
        Assert.NotNull(varying.SecondName);
        Assert.NotNull(varying.Precision);

        var numeric = SqlAssert.Expr<CastExpression>("cast(x as numeric(10, 2))").Type;
        Assert.NotNull(numeric.Scale);

        var tstz = SqlAssert.Expr<CastExpression>("cast(x as timestamp(6) with time zone)").Type;
        Assert.NotNull(tstz.WithKeyword);
        Assert.NotNull(tstz.Zone);
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
    public void Parses_niladic_functions(string sql)
    {
        Assert.Null(SqlAssert.Expr<NiladicFunctionExpression>(sql).OpenParen);
    }

    [Theory]
    [InlineData("current_time(3)")]
    [InlineData("current_timestamp(6)")]
    public void Parses_niladic_functions_with_precision(string sql)
    {
        Assert.NotNull(SqlAssert.Expr<NiladicFunctionExpression>(sql).Precision);
    }

    [Fact]
    public void Niladic_without_precision_rejects_parens()
    {
        Assert.Throws<SqlParseException>(() => Sql.Parse("select user()"));
        Assert.Throws<SqlParseException>(() => Sql.Parse("select current_date()"));
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
        Assert.Throws<SqlParseException>(() => Sql.Parse("select )"));
    }
}
