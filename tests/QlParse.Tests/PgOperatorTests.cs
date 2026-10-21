namespace QlParse.Tests;

// Postgres lexical tokens and operators (stream A): positional parameters, named/VARIADIC arguments,
// array subscripts and slices, custom and qualified operators, operator precedence, ILIKE, full text
// search combinators, keyword-named functions, and aggregate argument clauses.
public sealed class PgOperatorTests
{
    private const string SelectPrefix = "select ";
    private const int TwoItems = 2;

    private static string SpanText(string expression, SourceSpan span) =>
        $"{SelectPrefix}{expression}"[span.Position..span.End];

    private static string TokenText(string expression, SyntaxToken token) =>
        SpanText(expression, token.Span);

    // ---- Positional parameters ----

    [Fact]
    public void Parses_positional_parameter()
    {
        const string Sql = "$1";
        var parameter = SqlAssert.ExprPg<PositionalParameterExpression>(Sql);
        Assert.Equal(SyntaxKind.PositionalParameter, parameter.Parameter.Kind);
        Assert.Equal(Sql, SpanText(Sql, parameter.Span));
    }

    [Fact]
    public void Parses_positional_parameters_in_expressions_and_clauses()
    {
        const string Sql = "$1 + $23";
        var add = SqlAssert.ExprPg<BinaryExpression>(Sql);
        Assert.IsType<PositionalParameterExpression>(add.Left);
        Assert.Equal("$23", SpanText(Sql, add.Right.Span));
        var select = SqlAssert.SelectPg("select a from t where a = $1");
        var where = Assert.IsType<BinaryExpression>(select.Where?.Expression);
        Assert.IsType<PositionalParameterExpression>(where.Right);
    }

    [Fact]
    public void Positional_parameter_requires_postgres()
    {
        SqlAssert.ParseFails("select $1");
    }

    // ---- Named and VARIADIC arguments ----

    [Fact]
    public void Parses_named_arguments_with_both_arrows()
    {
        const string Sql = "f(a => 1, b := 2)";
        var call = SqlAssert.ExprPg<FunctionCallExpression>(Sql);
        var first = Assert.IsType<NamedArgumentExpression>(call.Arguments[0]);
        var second = Assert.IsType<NamedArgumentExpression>(call.Arguments[1]);
        Assert.Equal(SyntaxKind.EqualsGreaterToken, first.Arrow.Kind);
        Assert.Equal(SyntaxKind.ColonEqualsToken, second.Arrow.Kind);
        Assert.Equal("a => 1", SpanText(Sql, first.Span));
        Assert.Equal("b", TokenText(Sql, second.Name));
    }

    [Theory]
    [InlineData("select f(a => 1)")]
    [InlineData("select f(a := 1)")]
    [InlineData("select f(variadic arr)")]
    public void Named_and_variadic_arguments_require_postgres(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_variadic_argument()
    {
        const string Sql = "concat_ws(',', variadic array['a', 'b'])";
        var call = SqlAssert.ExprPg<FunctionCallExpression>(Sql);
        var variadic = Assert.IsType<VariadicArgumentExpression>(call.Arguments[1]);
        Assert.IsType<ArrayExpression>(variadic.Argument);
        Assert.Equal("variadic array['a', 'b']", SpanText(Sql, variadic.Span));
    }

    [Fact]
    public void Variadic_is_still_a_column_name_when_alone()
    {
        var call = SqlAssert.ExprPg<FunctionCallExpression>("f(variadic)");
        Assert.IsType<IdentifierExpression>(Assert.Single(call.Arguments));
    }

    // ---- Array subscripts and slices ----

    [Theory]
    [InlineData("a[1][2]")]
    [InlineData("(a)[1]")]
    [InlineData("f(x)[1]")]
    [InlineData("t.col[1]")]
    [InlineData("a[f(1, 2)]")]
    public void Parses_array_subscripts(string sql)
    {
        var subscript = SqlAssert.ExprPg<JsonAccessorExpression>(sql);
        Assert.Equal(sql, SpanText(sql, subscript.Span));
    }

    [Fact]
    public void Parses_subscript_of_parenthesized_cast()
    {
        const string Sql = "(x::int)[1]";
        var subscript = SqlAssert.ExprPg<JsonAccessorExpression>(Sql);
        Assert.IsType<ParenExpression>(subscript.Target);
        Assert.Equal(Sql, SpanText(Sql, subscript.Span));
    }

    [Fact]
    public void Parses_nested_subscript_target()
    {
        var outer = SqlAssert.ExprPg<JsonAccessorExpression>("a[1][2]");
        Assert.IsType<JsonAccessorExpression>(outer.Target);
    }

    [Theory]
    [InlineData("a[1:3]", true, true)]
    [InlineData("a[:3]", false, true)]
    [InlineData("a[2:]", true, false)]
    [InlineData("a[:]", false, false)]
    [InlineData("a[1:n]", true, true)]
    [InlineData("f(x)[i + 1:j]", true, true)]
    public void Parses_array_slices(string sql, bool hasLower, bool hasUpper)
    {
        var slice = SqlAssert.ExprPg<MdarraySliceExpression>(sql);
        var axis = Assert.Single(slice.Axes);
        Assert.NotNull(axis.Colon);
        Assert.Equal(hasLower, axis.Lower is not null);
        Assert.Equal(hasUpper, axis.Upper is not null);
        Assert.Equal(sql, SpanText(sql, slice.Span));
    }

    [Theory]
    [InlineData("select a[:3]")]
    [InlineData("select a[2:]")]
    public void Open_ended_slices_require_postgres(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    // ---- Custom and qualified operators ----

    [Theory]
    [InlineData("a +* b", "+*")]
    [InlineData("a *@- b", "*@-")]
    [InlineData("a ~~ b", "~~")]
    [InlineData("a !~~* b", "!~~*")]
    [InlineData("a ` b", "`")]
    public void Parses_custom_operator_sequences(string sql, string op)
    {
        var binary = SqlAssert.ExprPg<BinaryExpression>(sql);
        Assert.Equal(SyntaxKind.OperatorToken, binary.OperatorToken.Kind);
        Assert.Equal(op, TokenText(sql, binary.OperatorToken));
        Assert.Equal(sql, SpanText(sql, binary.Span));
    }

    [Fact]
    public void Parses_infix_qualified_operator()
    {
        const string Sql = "a operator(pg_catalog.+) b * c";
        var qualified = SqlAssert.ExprPg<QualifiedOperatorExpression>(Sql);
        Assert.NotNull(qualified.Left);
        Assert.Equal("pg_catalog", TokenText(Sql, Assert.Single(qualified.SchemaParts)));
        Assert.Equal(SyntaxKind.PlusToken, qualified.Operator.Kind);
        Assert.IsType<BinaryExpression>(qualified.Right);
        Assert.Equal(Sql, SpanText(Sql, qualified.Span));
    }

    [Fact]
    public void Parses_prefix_qualified_operator_without_schema()
    {
        const string Sql = "OPERATOR(@>) b";
        var qualified = SqlAssert.ExprPg<QualifiedOperatorExpression>(Sql);
        Assert.Null(qualified.Left);
        Assert.Empty(qualified.SchemaParts);
        Assert.Equal("@>", TokenText(Sql, qualified.Operator));
        Assert.Equal(Sql, SpanText(Sql, qualified.Span));
    }

    [Fact]
    public void Qualified_operator_binds_below_comparison()
    {
        var comparison = SqlAssert.ExprPg<BinaryExpression>("x = a operator(s.@>) b");
        Assert.Equal(SyntaxKind.EqualsToken, comparison.OperatorToken.Kind);
        Assert.IsType<QualifiedOperatorExpression>(comparison.Right);
    }

    [Fact]
    public void Qualified_operator_requires_postgres()
    {
        SqlAssert.ParseFails("select a operator(pg_catalog.+) b");
    }

    // ---- Operators: array, JSON, regex, range, inet, geometric, full text search ----

    [Theory]
    [InlineData("@>")]
    [InlineData("<@")]
    [InlineData("&&")]
    [InlineData("#>")]
    [InlineData("#>>")]
    [InlineData("?|")]
    [InlineData("?&")]
    [InlineData("#-")]
    [InlineData("@?")]
    [InlineData("@@")]
    [InlineData("~")]
    [InlineData("~*")]
    [InlineData("!~")]
    [InlineData("!~*")]
    [InlineData("<<")]
    [InlineData(">>")]
    [InlineData("&<")]
    [InlineData("&>")]
    [InlineData("-|-")]
    [InlineData("<<=")]
    [InlineData(">>=")]
    [InlineData("&")]
    [InlineData("#")]
    [InlineData("<->")]
    [InlineData("<^")]
    [InlineData(">^")]
    [InlineData("?#")]
    [InlineData("~=")]
    public void Parses_postgres_binary_operator(string op)
    {
        var sql = $"a {op} b";
        var binary = SqlAssert.ExprPg<BinaryExpression>(sql);
        Assert.Equal(SyntaxKind.OperatorToken, binary.OperatorToken.Kind);
        Assert.Equal(op, TokenText(sql, binary.OperatorToken));
        Assert.Equal(sql, SpanText(sql, binary.Span));
    }

    [Theory]
    [InlineData("a || b", SyntaxKind.ConcatToken)]
    [InlineData("j -> 'k'", SyntaxKind.JsonArrowToken)]
    [InlineData("j ->> 'k'", SyntaxKind.JsonTextArrowToken)]
    [InlineData("j -> k", SyntaxKind.JsonArrowToken)]
    [InlineData("j ? 'k'", SyntaxKind.QuestionMark)]
    [InlineData("j - 'k'", SyntaxKind.MinusToken)]
    [InlineData("i | j", SyntaxKind.BarToken)]
    [InlineData("7 % 3", SyntaxKind.PercentToken)]
    [InlineData("2 ^ 3", SyntaxKind.CaretToken)]
    public void Parses_postgres_operators_with_dedicated_token_kinds(string sql, SyntaxKind kind)
    {
        var binary = SqlAssert.ExprPg<BinaryExpression>(sql);
        Assert.Equal(kind, binary.OperatorToken.Kind);
        Assert.Equal(sql, SpanText(sql, binary.Span));
    }

    [Theory]
    [InlineData("select a @> b")]
    [InlineData("select a ~ 'x'")]
    [InlineData("select v @@ q")]
    [InlineData("select a ? 'k'")]
    [InlineData("select 7 % 3")]
    public void Postgres_operators_require_postgres(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("~ 5", "~")]
    [InlineData("@ -5", "@")]
    [InlineData("|/ 25.0", "|/")]
    [InlineData("||/ 27.0", "||/")]
    [InlineData("!! 'cat'::tsquery", "!!")]
    public void Parses_prefix_operators(string sql, string op)
    {
        var unary = SqlAssert.ExprPg<UnaryExpression>(sql);
        Assert.Equal(op, TokenText(sql, unary.OperatorToken));
        Assert.Equal(sql, SpanText(sql, unary.Span));
    }

    [Theory]
    [InlineData("v @@ q", "@@")]
    [InlineData("q1 && q2", "&&")]
    [InlineData("q1 || q2", "||")]
    [InlineData("q1 <-> q2", "<->")]
    public void Parses_full_text_search_operators(string sql, string op)
    {
        var binary = SqlAssert.ExprPg<BinaryExpression>(sql);
        Assert.Equal(op, TokenText(sql, binary.OperatorToken));
    }

    [Fact]
    public void Parses_tsquery_combinator_chain()
    {
        const string Sql = "to_tsvector(d) @@ (!! 'a'::tsquery && 'b'::tsquery || 'c'::tsquery)";
        var match = SqlAssert.ExprPg<BinaryExpression>(Sql);
        Assert.Equal("@@", TokenText(Sql, match.OperatorToken));
        var paren = Assert.IsType<ParenExpression>(match.Right);
        var or = Assert.IsType<BinaryExpression>(paren.Inner);
        Assert.Equal(SyntaxKind.ConcatToken, or.OperatorToken.Kind);
        Assert.Equal(Sql, SpanText(Sql, match.Span));
    }

    [Theory]
    [InlineData("a ilike 'x%'", false)]
    [InlineData("a not ilike 'x%' escape '!'", true)]
    public void Parses_ilike(string sql, bool negated)
    {
        var like = SqlAssert.ExprPg<LikeExpression>(sql);
        Assert.Equal("ilike", TokenText(sql, like.LikeKeyword));
        Assert.Equal(negated, like.NotKeyword is not null);
        Assert.Equal(sql, SpanText(sql, like.Span));
    }

    [Fact]
    public void Ilike_requires_postgres()
    {
        SqlAssert.ParseFails("select a ilike 'x%'");
    }

    [Fact]
    public void Parses_any_over_array()
    {
        const string Sql = "a = any($1)";
        var quantified = SqlAssert.ExprPg<QuantifiedArrayExpression>(Sql);
        Assert.IsType<PositionalParameterExpression>(quantified.Array);
        Assert.Equal(Sql, SpanText(Sql, quantified.Span));
        SqlAssert.ExprPg<QuantifiedSubqueryExpression>("a = any (select b from t)");
    }

    // ---- Precedence ----

    [Fact]
    public void Other_operators_bind_tighter_than_comparison()
    {
        var comparison = SqlAssert.ExprPg<BinaryExpression>("x = a @> b");
        Assert.Equal(SyntaxKind.EqualsToken, comparison.OperatorToken.Kind);
        Assert.IsType<BinaryExpression>(comparison.Right);
    }

    [Fact]
    public void Additive_operators_bind_tighter_than_other_operators()
    {
        var concat = SqlAssert.ExprPg<BinaryExpression>("a || b + c");
        Assert.Equal(SyntaxKind.ConcatToken, concat.OperatorToken.Kind);
        var add = Assert.IsType<BinaryExpression>(concat.Right);
        Assert.Equal(SyntaxKind.PlusToken, add.OperatorToken.Kind);
    }

    [Fact]
    public void Iso_concat_keeps_additive_precedence()
    {
        var add = SqlAssert.Expr<BinaryExpression>("a || b + c");
        Assert.Equal(SyntaxKind.PlusToken, add.OperatorToken.Kind);
    }

    [Fact]
    public void Exponent_is_left_associative_and_below_unary_minus()
    {
        var outer = SqlAssert.ExprPg<BinaryExpression>("2 ^ 3 ^ 2");
        Assert.IsType<BinaryExpression>(outer.Left);
        var negated = SqlAssert.ExprPg<BinaryExpression>("-2 ^ 2");
        Assert.IsType<UnaryExpression>(negated.Left);
        var product = SqlAssert.ExprPg<BinaryExpression>("2 * 3 ^ 2");
        Assert.Equal(SyntaxKind.Star, product.OperatorToken.Kind);
    }

    [Fact]
    public void Modulo_binds_like_multiplication()
    {
        var add = SqlAssert.ExprPg<BinaryExpression>("1 + 7 % 3");
        Assert.Equal(SyntaxKind.PlusToken, add.OperatorToken.Kind);
    }

    [Fact]
    public void Prefix_operator_takes_other_operator_precedence()
    {
        var and = SqlAssert.ExprPg<BinaryExpression>("~ a & b");
        Assert.IsType<UnaryExpression>(and.Left);
        var root = SqlAssert.ExprPg<UnaryExpression>("|/ a + b");
        Assert.IsType<BinaryExpression>(root.Expression);
    }

    [Fact]
    public void Sign_after_comparison_is_not_part_of_the_operator()
    {
        var comparison = SqlAssert.ExprPg<BinaryExpression>("a=-1");
        Assert.Equal(SyntaxKind.EqualsToken, comparison.OperatorToken.Kind);
        Assert.IsType<UnaryExpression>(comparison.Right);
    }

    // ---- Keyword-named functions ----

    [Theory]
    [InlineData("left(a, 2)", SyntaxKind.LeftKeyword)]
    [InlineData("right(a, 2)", SyntaxKind.RightKeyword)]
    [InlineData("lpad(a, 5)", SyntaxKind.Identifier)]
    [InlineData("rpad(a, 5, '*')", SyntaxKind.Identifier)]
    public void Parses_left_right_lpad_rpad(string sql, SyntaxKind nameKind)
    {
        var call = SqlAssert.ExprPg<FunctionCallExpression>(sql);
        Assert.Equal(nameKind, call.Name.Kind);
        Assert.Equal(sql, SpanText(sql, call.Span));
    }

    [Fact]
    public void Parses_unnest_in_expression_position()
    {
        const string Sql = "unnest(array[1, 2])";
        var call = SqlAssert.ExprPg<FunctionCallExpression>(Sql);
        Assert.Equal(SyntaxKind.UnnestKeyword, call.Name.Kind);
        Assert.Equal(Sql, SpanText(Sql, call.Span));
    }

    [Theory]
    [InlineData("select left(a, 2)")]
    [InlineData("select right(a, 2)")]
    [InlineData("select unnest(a)")]
    public void Keyword_named_functions_require_postgres(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Left_join_still_parses_under_postgres()
    {
        var select = SqlAssert.SelectPg("select left(a.x, 1) from a left join b on a.id = b.id");
        Assert.IsType<FunctionCallExpression>(select.SelectList[0].Expression);
    }

    // ---- Aggregate argument clauses ----

    [Fact]
    public void Parses_string_agg_with_order_by()
    {
        const string Sql = "string_agg(a, ',' order by a desc)";
        var aggregate = SqlAssert.ExprPg<AggregateCallExpression>(Sql);
        Assert.Equal(TwoItems, aggregate.Arguments.Count);
        Assert.NotNull(aggregate.OrderBy);
        Assert.Null(aggregate.SetQuantifier);
        Assert.Equal(Sql, SpanText(Sql, aggregate.Span));
    }

    [Fact]
    public void Aggregate_order_by_requires_postgres()
    {
        SqlAssert.ParseFails("select string_agg(a, ',' order by a) from t");
    }

    [Fact]
    public void Parses_distinct_aggregate_with_order_by_and_filter()
    {
        const string Sql = "array_agg(distinct a order by a) filter (where a > 0)";
        var aggregate = SqlAssert.ExprPg<AggregateCallExpression>(Sql);
        Assert.Equal(SyntaxKind.DistinctKeyword, aggregate.SetQuantifier?.Kind);
        Assert.NotNull(aggregate.OrderBy);
        Assert.NotNull(aggregate.Filter);
        Assert.Equal(Sql, SpanText(Sql, aggregate.Span));
    }

    [Fact]
    public void Parses_count_distinct()
    {
        var aggregate = SqlAssert.ExprPg<AggregateCallExpression>("count(distinct a)");
        Assert.Single(aggregate.Arguments);
        Assert.NotNull(aggregate.SetQuantifier);
    }

    [Fact]
    public void Parses_within_group()
    {
        const string Sql = "percentile_cont(0.5) within group (order by a)";
        var aggregate = SqlAssert.ExprPg<AggregateCallExpression>(Sql);
        Assert.NotNull(aggregate.WithinGroup);
        Assert.Single(aggregate.WithinGroup.OrderBy.Items);
        Assert.Equal("within group (order by a)", SpanText(Sql, aggregate.WithinGroup.Span));
        Assert.Equal(Sql, SpanText(Sql, aggregate.Span));
    }

    [Fact]
    public void Plain_postgres_calls_stay_function_calls()
    {
        var count = SqlAssert.ExprPg<FunctionCallExpression>("count(*) filter (where a > 1)");
        Assert.IsType<StarExpression>(Assert.Single(count.Arguments));
        Assert.NotNull(count.Filter);
        var rank = SqlAssert.ExprPg<FunctionCallExpression>("rank() over (order by a)");
        Assert.NotNull(rank.Over);
    }

    // ---- Visitor ----

    private const string VisitorSql =
        "select $1, f(a => 1, variadic b), a[1:2], a[:], x @> y, ~ z, a operator(s.+) b, "
        + "operator(-) c, a = any($2), string_agg(a, ',' order by a) filter (where a > 0) over (), "
        + "percentile_cont(0.5) within group (order by a), left(a, 1), unnest(a), a ilike 'x' from t";

    [Fact]
    public void Default_visitor_walks_postgres_operator_nodes()
    {
        new SqlVisitor().Visit(SqlAssert.SelectPg(VisitorSql));
    }

    private const string IdentifierSql =
        "select f(n => a), g(variadic b), c operator(+) d, e = any(h), count(distinct i) within group (order by j)";

    [Fact]
    public void Visitor_reaches_identifiers_inside_postgres_nodes()
    {
        var visitor = new IdentifierCollector(IdentifierSql);
        visitor.Visit(SqlAssert.SelectPg(IdentifierSql));
        Assert.Equal(["a", "b", "c", "d", "e", "h", "i", "j"], visitor.Names);
    }

    private sealed class IdentifierCollector(string source) : SqlVisitor
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Names => _names;

        public override void VisitIdentifierExpression(IdentifierExpression node) =>
            _names.Add(source[node.Span.Position..node.Span.End]);
    }
}
