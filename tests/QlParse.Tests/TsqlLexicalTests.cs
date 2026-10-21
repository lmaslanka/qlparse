namespace QlParse.Tests;

// SQL Server lexical layer (stream A): identifiers and quoting, literals, variables and system functions,
// pseudo-columns, and the T-SQL operators. Batches and statement terminators are in TsqlLexicalTests.Batch.cs.
public sealed partial class TsqlLexicalTests
{
    private const string SelectPrefix = "select ";
    private const string EscapedBracketName = "[a]]b]";
    private const string HexString = "x'0A'";
    private const string ColonName = ":name";

    private static SyntaxKind[] Kinds(string sql, SqlOptions options = SqlOptions.SqlServer)
    {
        var result = Sql.Lex(sql, options);
        Assert.Null(result.Error);
        return [.. result.Tokens.Select(token => token.Kind)];
    }

    private static string Text(string sql, SyntaxToken token) => sql[token.Position..token.Span.End];

    private static string ExprText(string expression, SourceSpan span) =>
        $"{SelectPrefix}{expression}"[span.Position..span.End];

    private static string ExprToken(string expression, SyntaxToken token) => ExprText(expression, token.Span);

    private static void LexFails(string sql) => Assert.NotNull(Sql.Lex(sql, SqlOptions.SqlServer).Error);

    // ---- Identifiers and quoting ----

    [Fact]
    public void Lexes_bracket_identifiers_with_escaped_close_bracket()
    {
        const string sql = "select [My Table].[a]]b] from [dbo].[t]";
        var tokens = Sql.Lex(sql, SqlOptions.SqlServer).Tokens;
        Assert.Equal(SyntaxKind.Identifier, tokens[1].Kind);
        Assert.Equal("[My Table]", Text(sql, tokens[1]));
        Assert.Equal(EscapedBracketName, Text(sql, tokens[3]));
        var select = SqlAssert.SelectTsql(sql);
        var member = Assert.IsType<MemberAccessExpression>(select.SelectList[0].Expression);
        Assert.Equal(EscapedBracketName, Text(sql, member.Member));
        var table = Assert.IsType<TableReference>(select.From);
        Assert.Equal(["[dbo]", "[t]"], table.NameParts.Select(part => Text(sql, part)));
    }

    [Fact]
    public void Bracket_identifier_is_never_a_keyword()
    {
        const string sql = "select [select] from [from]";
        var select = SqlAssert.SelectTsql(sql);
        var column = Assert.IsType<IdentifierExpression>(select.SelectList[0].Expression);
        Assert.Equal("[select]", Text(sql, column.Identifier));
    }

    [Theory]
    [InlineData("select []")]
    [InlineData("select [abc")]
    [InlineData("select [a]]")]
    public void Rejects_empty_or_unterminated_bracket_identifiers(string sql)
    {
        LexFails(sql);
    }

    [Fact]
    public void Brackets_keep_their_iso_meaning_without_sql_server()
    {
        Assert.Equal(
            [SyntaxKind.SelectKeyword, SyntaxKind.OpenBracket, SyntaxKind.Identifier, SyntaxKind.CloseBracket, SyntaxKind.EndOfFile],
            Kinds("select [a]", SqlOptions.None));
        SqlAssert.ParseFails("select [a] from t");
    }

    [Fact]
    public void Double_quotes_delimit_identifiers_as_with_quoted_identifier_on()
    {
        const string sql = "select \"a b\" from \"t\"";
        var column = Assert.IsType<IdentifierExpression>(SqlAssert.SelectTsql(sql).SelectList[0].Expression);
        Assert.Equal("\"a b\"", Text(sql, column.Identifier));
    }

    [Fact]
    public void Lexes_temporary_object_names()
    {
        const string sql = "select a from #t join ##g on #t.id = ##g.id";
        var select = SqlAssert.SelectTsql(sql);
        var table = Assert.IsType<TableReference>(select.From);
        Assert.Equal("#t", Text(sql, Assert.Single(table.NameParts)));
        var join = Assert.Single(select.Joins);
        Assert.Equal("##g", Text(sql, Assert.Single(Assert.IsType<TableReference>(join.Table).NameParts)));
        Assert.Equal([SyntaxKind.Identifier, SyntaxKind.EndOfFile], Kinds("#"));
        SqlAssert.ParseFails("select a from #t");
    }

    [Fact]
    public void Lexes_extended_identifier_characters()
    {
        const string sql = "select a#b, c@d, e$f, _g from t";
        var select = SqlAssert.SelectTsql(sql);
        Assert.Equal(
            ["a#b", "c@d", "e$f", "_g"],
            select.SelectList.Select(item => Text(sql, Assert.IsType<IdentifierExpression>(item.Expression).Identifier)));
        SqlAssert.ParseFails("select a#b from t");
    }

    // ---- Literals ----

    [Fact]
    public void National_strings_are_string_literals()
    {
        const string expression = "N'abc'";
        var literal = SqlAssert.ExprTsql<LiteralExpression>(expression);
        Assert.Equal(SyntaxKind.String, literal.Literal.Kind);
        Assert.Equal(expression, ExprToken(expression, literal.Literal));
        Assert.Equal(SyntaxKind.String, SqlAssert.Expr<LiteralExpression>(expression).Literal.Kind);
    }

    [Fact]
    public void Only_the_national_prefix_starts_a_string()
    {
        Assert.Equal([SyntaxKind.Identifier, SyntaxKind.String, SyntaxKind.EndOfFile], Kinds(HexString));
        Assert.Equal([SyntaxKind.Identifier, SyntaxKind.String, SyntaxKind.EndOfFile], Kinds("b'01'"));
        Assert.Equal([SyntaxKind.Identifier, SyntaxKind.AmpersandToken, SyntaxKind.String, SyntaxKind.EndOfFile], Kinds("U&'x'"));
        Assert.Equal([SyntaxKind.String, SyntaxKind.EndOfFile], Kinds(HexString, SqlOptions.None));
    }

    [Theory]
    [InlineData("0x0A1B")]
    [InlineData("0XFF")]
    [InlineData("0x")]
    public void Lexes_binary_literals(string expression)
    {
        var literal = SqlAssert.ExprTsql<LiteralExpression>(expression);
        Assert.Equal(SyntaxKind.BinaryLiteral, literal.Literal.Kind);
        Assert.Equal(expression, ExprToken(expression, literal.Literal));
    }

    [Fact]
    public void Binary_literal_keeps_its_iso_meaning_without_sql_server()
    {
        var item = SqlAssert.Select("select 0x1F").SelectList[0];
        Assert.Equal(SyntaxKind.Number, Assert.IsType<LiteralExpression>(item.Expression).Literal.Kind);
        Assert.NotNull(item.Alias);
    }

    [Theory]
    [InlineData("$12.50")]
    [InlineData("$542023")]
    [InlineData("$.5")]
    [InlineData("£10.25")]
    [InlineData("¥1000")]
    public void Lexes_money_literals(string expression)
    {
        var literal = SqlAssert.ExprTsql<LiteralExpression>(expression);
        Assert.Equal(SyntaxKind.MoneyLiteral, literal.Literal.Kind);
        Assert.Equal(expression, ExprToken(expression, literal.Literal));
    }

    [Fact]
    public void Negative_money_is_a_unary_minus()
    {
        var negative = SqlAssert.ExprTsql<UnaryExpression>("-$45.56");
        Assert.Equal(SyntaxKind.MoneyLiteral, Assert.IsType<LiteralExpression>(negative.Expression).Literal.Kind);
        SqlAssert.ParseFails("select $12");
    }

    // ---- Variables and system functions ----

    [Fact]
    public void Variables_are_embedded_host_names()
    {
        const string expression = "@total + @a#b$c";
        var add = SqlAssert.ExprTsql<BinaryExpression>(expression);
        Assert.Equal("@total", ExprToken(expression, Assert.IsType<EmbeddedHostExpression>(add.Left).Name));
        Assert.Equal("@a#b$c", ExprToken(expression, Assert.IsType<EmbeddedHostExpression>(add.Right).Name));
        SqlAssert.ParseFails("select @total");
        LexFails("select @ x");
    }

    [Theory]
    [InlineData("@@ROWCOUNT")]
    [InlineData("@@error")]
    [InlineData("@@IDENTITY")]
    [InlineData("@@TRANCOUNT")]
    [InlineData("@@SPID")]
    [InlineData("@@VERSION")]
    [InlineData("@@SERVERNAME")]
    [InlineData("@@FETCH_STATUS")]
    public void System_functions_are_global_variable_expressions(string expression)
    {
        var global = SqlAssert.ExprTsql<TsqlGlobalVariableExpression>(expression);
        Assert.Equal(SyntaxKind.EmbeddedHost, global.Name.Kind);
        Assert.Equal(expression, ExprToken(expression, global.Name));
    }

    [Fact]
    public void System_functions_compose_in_expressions()
    {
        var select = SqlAssert.SelectTsql("select a from t where @@ROWCOUNT > 0 and @@ERROR = 0");
        var and = Assert.IsType<BinaryExpression>(select.Where?.Expression);
        Assert.IsType<TsqlGlobalVariableExpression>(Assert.IsType<BinaryExpression>(and.Left).Left);
        SqlAssert.ParseFails("select @@ROWCOUNT", SqlOptions.AtParameters);
    }

    // ---- Comments ----

    [Fact]
    public void Block_comments_nest()
    {
        const string sql = "select /* outer /* inner */ still a comment */ 1";
        Assert.Equal([SyntaxKind.SelectKeyword, SyntaxKind.Number, SyntaxKind.EndOfFile], Kinds(sql));
        Assert.Equal(SyntaxKind.BlockCommentTrivia, Assert.Single(Sql.Lex(sql, SqlOptions.SqlServer).Trivia).Kind);
        LexFails("select /* outer /* inner */ 1");
        Assert.Equal(Kinds(sql), Kinds(sql, SqlOptions.None));
    }

    // ---- Pseudo-columns ----

    [Theory]
    [InlineData("$IDENTITY")]
    [InlineData("$rowguid")]
    [InlineData("$node_id")]
    [InlineData("$edge_id")]
    [InlineData("$from_id")]
    [InlineData("$to_id")]
    [InlineData("$action")]
    public void Pseudo_columns_are_identifiers(string expression)
    {
        var column = SqlAssert.ExprTsql<IdentifierExpression>(expression);
        Assert.Equal(expression, ExprToken(expression, column.Identifier));
    }

    [Fact]
    public void Pseudo_columns_qualify_like_columns()
    {
        const string expression = "e.$from_id";
        var member = SqlAssert.ExprTsql<MemberAccessExpression>(expression);
        Assert.Equal("$from_id", ExprToken(expression, member.Member));
        SqlAssert.ParseFails("select $identity from t");
        SqlAssert.ParseFails("select $unknown from t", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_partition_function_call()
    {
        const string expression = "$PARTITION.RangePF1(a + 1)";
        var call = SqlAssert.ExprTsql<TsqlPartitionFunctionCallExpression>(expression);
        Assert.Null(call.Database);
        Assert.Equal("$PARTITION", ExprToken(expression, call.PartitionKeyword));
        Assert.Equal("RangePF1", ExprToken(expression, call.FunctionName));
        Assert.Equal("a + 1", ExprText(expression, call.Argument.Span));
        Assert.Equal(expression, ExprText(expression, call.Span));
    }

    [Fact]
    public void Parses_database_qualified_partition_function_call()
    {
        const string expression = "AdventureWorks.$partition.pf(10)";
        var call = SqlAssert.ExprTsql<TsqlPartitionFunctionCallExpression>(expression);
        Assert.Equal("AdventureWorks", ExprToken(expression, Assert.NotNull(call.Database)));
        Assert.NotNull(call.DatabaseDot);
        Assert.Equal(expression, ExprText(expression, call.Span));
        Assert.IsType<MethodInvocationExpression>(SqlAssert.ExprTsql("p.pf(10)"));
        SqlAssert.ParseFails("select $PARTITION.pf(10)");
    }

    // ---- Operators ----

    [Theory]
    [InlineData("+=")]
    [InlineData("-=")]
    [InlineData("*=")]
    [InlineData("/=")]
    [InlineData("%=")]
    [InlineData("&=")]
    [InlineData("|=")]
    [InlineData("^=")]
    [InlineData("||=")]
    public void Lexes_compound_assignment_operators(string op)
    {
        var sql = $"@v {op} 1";
        var tokens = Sql.Lex(sql, SqlOptions.SqlServer).Tokens;
        Assert.Equal(SyntaxKind.CompoundAssignmentToken, tokens[1].Kind);
        Assert.Equal(op, Text(sql, tokens[1]));
    }

    [Fact]
    public void Compound_assignment_lexes_as_iso_tokens_without_sql_server()
    {
        Assert.Equal([SyntaxKind.PlusToken, SyntaxKind.EqualsToken, SyntaxKind.EndOfFile], Kinds("+=", SqlOptions.None));
        Assert.Equal([SyntaxKind.Star, SyntaxKind.EqualsToken, SyntaxKind.EndOfFile], Kinds("*=", SqlOptions.None));
    }

    [Fact]
    public void Lexes_operator_tokens()
    {
        Assert.Equal(
            [
                SyntaxKind.NotEqualsToken, SyntaxKind.NotLessToken, SyntaxKind.NotGreaterToken, SyntaxKind.PercentToken,
                SyntaxKind.AmpersandToken, SyntaxKind.BarToken, SyntaxKind.CaretToken, SyntaxKind.TildeToken,
                SyntaxKind.EqualsStarToken, SyntaxKind.DoubleColonToken, SyntaxKind.ConcatToken, SyntaxKind.EndOfFile,
            ],
            Kinds("!= !< !> % & | ^ ~ =* :: ||"));
        LexFails("select a !! b");
    }

    [Fact]
    public void Scope_resolution_operator_lexes_for_static_methods()
    {
        const string sql = "geography::Point(1, 2, 4326)";
        Assert.Equal(
            [SyntaxKind.Identifier, SyntaxKind.DoubleColonToken, SyntaxKind.Identifier, SyntaxKind.OpenParen],
            Kinds(sql)[..4]);
    }

    [Fact]
    public void Colon_before_a_name_is_not_a_host_variable()
    {
        Assert.Equal([SyntaxKind.ColonToken, SyntaxKind.Identifier, SyntaxKind.EndOfFile], Kinds(ColonName));
        Assert.Equal([SyntaxKind.EmbeddedHost, SyntaxKind.EndOfFile], Kinds(ColonName, SqlOptions.None));
    }

    [Theory]
    [InlineData("a != b", SyntaxKind.NotEqualsToken)]
    [InlineData("a !< b", SyntaxKind.NotLessToken)]
    [InlineData("a !> b", SyntaxKind.NotGreaterToken)]
    public void Parses_non_iso_comparisons(string expression, SyntaxKind kind)
    {
        var comparison = SqlAssert.ExprTsql<BinaryExpression>(expression);
        Assert.Equal(kind, comparison.OperatorToken.Kind);
        Assert.IsType<IdentifierExpression>(comparison.Left);
    }

    [Theory]
    [InlineData("select a !< b")]
    [InlineData("select a !> b")]
    [InlineData("select a % b")]
    [InlineData("select a & b")]
    [InlineData("select a | b")]
    [InlineData("select ~a")]
    [InlineData("select a *= b")]
    public void T_sql_operators_are_rejected_without_sql_server(string sql)
    {
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Modulo_binds_like_multiplication()
    {
        var add = SqlAssert.ExprTsql<BinaryExpression>("a + b % c");
        Assert.Equal(SyntaxKind.PlusToken, add.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.PercentToken, Assert.IsType<BinaryExpression>(add.Right).OperatorToken.Kind);
    }

    [Fact]
    public void Bitwise_operators_share_the_additive_level_left_to_right()
    {
        var or = SqlAssert.ExprTsql<BinaryExpression>("a + b & c | d ^ e");
        Assert.Equal(SyntaxKind.CaretToken, or.OperatorToken.Kind);
        var bar = Assert.IsType<BinaryExpression>(or.Left);
        Assert.Equal(SyntaxKind.BarToken, bar.OperatorToken.Kind);
        var and = Assert.IsType<BinaryExpression>(bar.Left);
        Assert.Equal(SyntaxKind.AmpersandToken, and.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.PlusToken, Assert.IsType<BinaryExpression>(and.Left).OperatorToken.Kind);
    }

    [Fact]
    public void Bitwise_operators_bind_tighter_than_comparison_and_looser_than_multiplication()
    {
        var equals = SqlAssert.ExprTsql<BinaryExpression>("flags & 4 * 2 = 8");
        Assert.Equal(SyntaxKind.EqualsToken, equals.OperatorToken.Kind);
        var and = Assert.IsType<BinaryExpression>(equals.Left);
        Assert.Equal(SyntaxKind.AmpersandToken, and.OperatorToken.Kind);
        Assert.Equal(SyntaxKind.Star, Assert.IsType<BinaryExpression>(and.Right).OperatorToken.Kind);
    }

    [Fact]
    public void Bitwise_not_is_a_unary_operator_binding_tightest()
    {
        var add = SqlAssert.ExprTsql<BinaryExpression>("~a * 2");
        var not = Assert.IsType<UnaryExpression>(add.Left);
        Assert.Equal(SyntaxKind.TildeToken, not.OperatorToken.Kind);
        Assert.IsType<IdentifierExpression>(not.Expression);
    }

    [Fact]
    public void Plus_concatenates_strings()
    {
        const string expression = "'a' + N'b' + c";
        var concat = SqlAssert.ExprTsql<BinaryExpression>(expression);
        Assert.Equal(SyntaxKind.PlusToken, concat.OperatorToken.Kind);
        var inner = Assert.IsType<BinaryExpression>(concat.Left);
        Assert.Equal("N'b'", ExprToken(expression, Assert.IsType<LiteralExpression>(inner.Right).Literal));
    }

    [Theory]
    [InlineData("a.x *= b.x", SyntaxKind.CompoundAssignmentToken)]
    [InlineData("a.x =* b.x", SyntaxKind.EqualsStarToken)]
    public void Parses_legacy_outer_join_comparisons(string condition, SyntaxKind kind)
    {
        var select = SqlAssert.SelectTsql($"select * from a, b where {condition} and a.y = 1");
        var and = Assert.IsType<BinaryExpression>(select.Where?.Expression);
        var join = Assert.IsType<BinaryExpression>(and.Left);
        Assert.Equal(kind, join.OperatorToken.Kind);
        Assert.IsType<MemberAccessExpression>(join.Right);
    }

    [Fact]
    public void Other_compound_assignments_end_an_expression()
    {
        SqlAssert.ParseFails("select a += 1", SqlOptions.SqlServer);
    }

    [Theory]
    [InlineData("a is distinct from b", null)]
    [InlineData("a is not distinct from b", "not")]
    public void Parses_is_distinct_from(string expression, string? notText)
    {
        var distinct = SqlAssert.ExprTsql<DistinctFromExpression>(expression);
        Assert.Equal(notText, distinct.NotKeyword is { } keyword ? ExprToken(expression, keyword) : null);
        Assert.IsType<IdentifierExpression>(distinct.Right);
    }

    [Theory]
    [InlineData("name like '[a-z]%'")]
    [InlineData("name not like '[^x]_[0-9]' escape '!'")]
    public void Like_character_classes_stay_inside_the_pattern_string(string expression)
    {
        var like = SqlAssert.ExprTsql<LikeExpression>(expression);
        Assert.Equal(SyntaxKind.String, Assert.IsType<LiteralExpression>(like.Pattern).Literal.Kind);
    }

    [Fact]
    public void Visitor_walks_t_sql_expressions()
    {
        var select = SqlAssert.SelectTsql("select $PARTITION.pf(@@ROWCOUNT), ~a & 0x01 from #t");
        new SqlVisitor().Visit(select);
        Assert.Equal(2, select.SelectList.Count);
    }
}
