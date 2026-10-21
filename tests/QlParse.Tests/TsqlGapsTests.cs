namespace QlParse.Tests;

// SQL Server gap closing (workstream H): QUOTED_IDENTIFIER OFF, non-reserved words as names, legacy table hints,
// omitted name parts in expressions, ORDER BY after a parenthesised set operation and `.5` literals.
public sealed partial class TsqlGapsTests
{
    private const SqlOptions QuotedIdentifierOff = SqlOptions.SqlServer | SqlOptions.QuotedIdentifierOff;
    private const string DoubleQuotedSelect = "select \"it\"\"s\", \"\" from t where a = \"x\"";
    private const string NonReservedColumns = "select date, time, timestamp, position, filter, window, match, offset, "
        + "only, using, cast, trim, grouping, absent, search, cycle, upper from t";

    private static string Text(string sql, SyntaxToken token) => sql[token.Position..token.Span.End];

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Words(string sql, IEnumerable<SyntaxToken> tokens) => string.Join(" ", tokens.Select(token => Text(sql, token)));

    private static T Whole<T>(string sql) where T : Query
    {
        var statement = SqlAssert.ParseTsql<T>(sql);
        Assert.Equal(sql, Text(sql, statement.Span));
        return statement;
    }

    // ---- QUOTED_IDENTIFIER OFF ----

    [Fact]
    public void Quoted_identifier_off_makes_double_quotes_strings()
    {
        var result = Sql.Parse(DoubleQuotedSelect, QuotedIdentifierOff);
        Assert.Null(result.Error);
        var select = Assert.IsType<SelectStatement>(result.Root);
        var first = Assert.IsType<LiteralExpression>(select.SelectList[0].Expression);
        Assert.Equal(SyntaxKind.String, first.Literal.Kind);
        Assert.Equal("\"it\"\"s\"", Text(DoubleQuotedSelect, first.Literal));
        Assert.Equal("\"\"", Text(DoubleQuotedSelect, Assert.IsType<LiteralExpression>(select.SelectList[1].Expression).Literal));
        var where = Assert.IsType<BinaryExpression>(select.Where!.Expression);
        Assert.IsType<LiteralExpression>(where.Right);
    }

    [Fact]
    public void Double_quotes_stay_identifiers_unless_both_flags_are_set()
    {
        const string sql = "select \"a b\" from \"t\"";
        Assert.All(
            new[] { SqlOptions.None, SqlOptions.SqlServer, SqlOptions.QuotedIdentifierOff, SqlOptions.Postgres | SqlOptions.QuotedIdentifierOff },
            options => Assert.Equal(SyntaxKind.Identifier, Sql.Lex(sql, options).Tokens[1].Kind));
        Assert.Equal(SyntaxKind.String, Sql.Lex(sql, QuotedIdentifierOff).Tokens[1].Kind);
        Assert.NotNull(Sql.Parse(DoubleQuotedSelect).Error);
    }

    [Fact]
    public void Quoted_identifier_off_keeps_brackets_and_rejects_an_open_string()
    {
        var result = Sql.Parse("print \"done\" select [x y] from [t]", QuotedIdentifierOff);
        var batch = Assert.IsType<TsqlBatch>(result.Root);
        Assert.IsType<TsqlPrintStatement>(batch.Statements[0]);
        SqlAssert.ParseFails("select \"abc", QuotedIdentifierOff);
    }

    // ---- Non-reserved words ----

    [Fact]
    public void Non_reserved_iso_keywords_are_plain_names()
    {
        var select = SqlAssert.SelectTsql(NonReservedColumns);
        Assert.Equal(17, select.SelectList.Count);
        Assert.All(select.SelectList, item => Assert.Equal(SyntaxKind.Identifier, Assert.IsType<IdentifierExpression>(item.Expression).Identifier.Kind));
        SqlAssert.ParseFails(NonReservedColumns);
    }

    [Fact]
    public void Lexer_keeps_only_reserved_words_as_keywords()
    {
        const string sql = "select date from t where position = 1 order by window";
        var kinds = Sql.Lex(sql, SqlOptions.SqlServer).Tokens.Select(token => token.Kind).ToArray();
        Assert.Equal(
            [SyntaxKind.SelectKeyword, SyntaxKind.Identifier, SyntaxKind.FromKeyword, SyntaxKind.Identifier, SyntaxKind.WhereKeyword,
                SyntaxKind.Identifier, SyntaxKind.EqualsToken, SyntaxKind.Number, SyntaxKind.OrderKeyword, SyntaxKind.ByKeyword,
                SyntaxKind.Identifier, SyntaxKind.EndOfFile],
            kinds);
        Assert.Equal(SyntaxKind.DateKeyword, SqlAssert.Lex(sql).Tokens[1].Kind);
    }

    [Theory]
    [InlineData("create table t (date date, time time(7), position int, filter int, timestamp timestamp)")]
    [InlineData("insert into t (date, time, position) values (1, 2, 3)")]
    [InlineData("update t set date = getdate(), time = 1 where position = 2")]
    [InlineData("select d.date, d.offset, d.match from dbo.Orders as d where d.date > '2020-01-01' order by d.date")]
    [InlineData("select * from date d join time t on d.id = t.id")]
    [InlineData("select cast(a as date) date, position = 1, a time from t")]
    [InlineData("create procedure p @date date, @time time as select @date")]
    public void Non_reserved_words_work_in_every_name_position(string sql) =>
        Assert.Null(Sql.Parse(sql, SqlOptions.SqlServer).Error);

    [Fact]
    public void Words_with_t_sql_meaning_are_matched_by_text()
    {
        var cast = SqlAssert.ExprTsql<CastExpression>("cast(a as date)");
        Assert.Equal(SyntaxKind.Identifier, cast.CastKeyword.Kind);
        Assert.Equal(SyntaxKind.Identifier, cast.Type.Name.Kind);
        SqlAssert.ExprTsql<TrimExpression>("trim('x' from a)");
        SqlAssert.ExprTsql<TsqlAtTimeZoneExpression>("a at time zone 'UTC'");
        var upper = SqlAssert.ExprTsql<FunctionCallExpression>("upper(name)");
        Assert.Equal(SyntaxKind.Identifier, upper.Name.Kind);
        var paged = SqlAssert.SelectTsql("select a from t order by a offset 10 rows fetch next 5 rows only");
        Assert.NotNull(paged.Offset);
        Assert.NotNull(paged.Fetch);
        Assert.NotNull(SqlAssert.SelectTsql("select sum(a) over w from t window w as (order by b)").Window);
        Assert.NotNull(SqlAssert.SelectTsql("select a from t group by grouping sets ((a), ())").GroupBy);
        SqlAssert.ParseTsql<TsqlMergeStatement>("merge t using s on t.a = s.a when matched then delete;");
    }

    [Fact]
    public void Window_and_using_are_aliases_only_when_no_clause_follows()
    {
        var aliased = SqlAssert.SelectTsql("select window.a from t as window");
        Assert.Equal("window", Text("select window.a from t as window", Assert.IsType<TableReference>(aliased.From).Alias!.Value));
        var bare = SqlAssert.SelectTsql("select a from t window");
        Assert.NotNull(Assert.IsType<TableReference>(bare.From).Alias);
        Assert.Null(bare.Window);
    }

    [Fact]
    public void Sequence_options_take_non_reserved_words()
    {
        const string sql = "create sequence dbo.s as bigint start with 1 increment by 1 minvalue 1 maxvalue 1000 cycle cache 10";
        var sequence = SqlAssert.ParseTsql<CreateSequenceStatement>(sql);
        Assert.Equal(6, sequence.Options.Count);
        Assert.Equal("cache 10", Text(sql, sequence.Options[^1].Span));
        var bare = SqlAssert.ParseTsql<CreateSequenceStatement>("create sequence s start with 1 no cycle no cache cache");
        Assert.Equal(4, bare.Options.Count);
        Assert.Null(bare.Options[^1].Value);
        SqlAssert.ParseFails("create sequence s start with 1 no cache");
    }

    // ---- FROM items and expressions ----

    [Theory]
    [InlineData("select * from t (nolock) where a = 1", "t", null)]
    [InlineData("select * from dbo.t x (nolock, readpast)", "x", null)]
    [InlineData("select * from t as x (holdlock) join u on 1 = 1", "x", null)]
    public void Legacy_table_hints_need_no_with(string sql, string name, string? expectedWith)
    {
        var select = SqlAssert.SelectTsql(sql);
        var hinted = Assert.IsType<TsqlHintedTable>(select.From);
        Assert.Equal(expectedWith, hinted.WithKeyword is { } with ? Text(sql, with) : null);
        var table = Assert.IsType<TableReference>(hinted.Table);
        Assert.Equal(name, Text(sql, table.Alias ?? table.NameParts[^1]));
        Assert.NotEmpty(hinted.Hints);
    }

    [Fact]
    public void A_call_or_column_list_is_not_a_legacy_hint()
    {
        Assert.IsType<TableFunction>(SqlAssert.SelectTsql("select * from dbo.f (x)").From);
        Assert.IsType<TableFunction>(SqlAssert.SelectTsql("select * from f (nolock + 1)").From);
        Assert.IsType<TableFunction>(Assert.IsType<SelectStatement>(Sql.Parse("select * from t (nolock)").Root).From);
        Assert.Equal(2, Assert.IsType<TableReference>(SqlAssert.Select("select * from t as x (a, b)").From).Columns!.Count);
    }

    [Theory]
    [InlineData("db..t.col")]
    [InlineData("srv.db..t.col")]
    public void Omitted_name_parts_work_in_expressions(string expression)
    {
        var sql = "select " + expression + " from " + expression[..expression.LastIndexOf('.')];
        var select = SqlAssert.SelectTsql(sql);
        var member = Assert.IsType<MemberAccessExpression>(select.SelectList[0].Expression);
        Assert.Equal("col", Text(sql, member.Member));
        var table = Assert.IsType<MemberAccessExpression>(member.Target);
        Assert.Equal(0, Assert.IsType<MemberAccessExpression>(table.Target).Member.Length);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("(select a from t union select a from u) order by a", false)]
    [InlineData("select a from t union (select a from u) order by a", false)]
    [InlineData("(select a from t) union all (select b from u) order by 1 offset 5 rows fetch next 2 rows only", true)]
    public void Parenthesised_set_operations_take_order_by(string sql, bool paged)
    {
        var ordered = SqlAssert.ParseTsql<OrderedQuery>(sql);
        Assert.NotNull(ordered.OrderBy);
        Assert.Equal(paged, ordered.Fetch is not null);
        Assert.Equal(sql, Text(sql, ordered.Span));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void A_fraction_without_integer_part_is_one_number()
    {
        const string sql = "select .5, 1 + .25e2";
        var select = SqlAssert.SelectTsql(sql);
        Assert.Equal(".5", Text(sql, Assert.IsType<LiteralExpression>(select.SelectList[0].Expression).Literal));
        var sum = Assert.IsType<BinaryExpression>(select.SelectList[1].Expression);
        Assert.Equal(".25e2", Text(sql, Assert.IsType<LiteralExpression>(sum.Right).Literal));
        SqlAssert.ParseFails("select .5");
        SqlAssert.ParseFails("select . 5", SqlOptions.SqlServer);
    }
}
