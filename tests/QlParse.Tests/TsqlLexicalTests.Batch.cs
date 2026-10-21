namespace QlParse.Tests;

// SQL Server batches (stream A): optional `;` terminators, GO batch separators, and the places where T-SQL
// still requires a terminator.
public sealed partial class TsqlLexicalTests
{
    private const string Semicolon = ";";
    private const string SemicolonText = "semicolon";
    private const int TwoStatements = 2;
    private const string TwoBatches = "select 1\nGO\nselect 2\n  go 3  -- run three times\n";
    private const string Terminated = "select 1;"
        + " select 2;"
        + Semicolon
        ;
    private const string LeadingTerminators = Semicolon
        + "; select 1"
        ;
    private const string TerminatedWith = "select 1;"
        + " with c as (select 1 as x) select x from c"
        ;
    private const string TerminatedMerge = "merge into t using s on t.id = s.id when matched then delete"
        + Semicolon
        ;
    private const string SelectGo = "select go";

    private static string SpanText(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string ParseError(string sql)
    {
        var error = Sql.Parse(sql, SqlOptions.SqlServer).Error;
        Assert.NotNull(error);
        return error.Message;
    }

    [Fact]
    public void Single_statement_is_returned_as_itself()
    {
        SqlAssert.ParseTsql<SelectStatement>("select 1");
        SqlAssert.ParseTsql<SelectStatement>(LeadingTerminators);
    }

    [Fact]
    public void Statements_separated_by_whitespace_form_a_batch()
    {
        const string sql = "select a from t insert into u values (1) update t set a = 1 delete from t where a = 1 select 2";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Collection(
            batch.Statements,
            statement => Assert.IsType<SelectStatement>(statement),
            statement => Assert.IsType<InsertStatement>(statement),
            statement => Assert.IsType<UpdateStatement>(statement),
            statement => Assert.IsType<DeleteStatement>(statement),
            statement => Assert.IsType<SelectStatement>(statement));
        Assert.Empty(batch.Semicolons);
        Assert.Null(batch.Separator);
        Assert.Equal(sql, SpanText(sql, batch.Span));
        Assert.Equal("select a from t", SpanText(sql, batch.Statements[0].Span));
        SqlAssert.ParseFails("select 1 select 2");
    }

    [Fact]
    public void Batch_records_every_terminator()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>(Terminated);
        Assert.Equal(TwoStatements, batch.Statements.Count);
        Assert.Equal(3, batch.Semicolons.Count);
        Assert.Equal(Terminated, SpanText(Terminated, batch.Span));
    }

    [Fact]
    public void Reserved_words_end_an_implicit_alias()
    {
        const string sql = "select a x from t y delete from t z";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var select = Assert.IsType<SelectStatement>(batch.Statements[0]);
        Assert.Equal("x", SpanText(sql, Assert.NotNull(select.SelectList[0].Alias).Span));
        Assert.Equal("y", SpanText(sql, Assert.NotNull(Assert.IsType<TableReference>(select.From).Alias).Span));
        Assert.IsType<DeleteStatement>(batch.Statements[1]);
    }

    [Fact]
    public void Delimited_reserved_words_still_alias()
    {
        const string sql = "select a [insert] from t [delete]";
        var select = SqlAssert.SelectTsql(sql);
        Assert.Equal("[insert]", SpanText(sql, Assert.NotNull(select.SelectList[0].Alias).Span));
        Assert.Equal("[delete]", SpanText(sql, Assert.NotNull(Assert.IsType<TableReference>(select.From).Alias).Span));
        Assert.NotNull(SqlAssert.Select("select a insert from t").SelectList[0].Alias);
    }

    [Fact]
    public void Go_lines_split_the_script_into_batches()
    {
        var script = SqlAssert.ParseTsql<TsqlScript>(TwoBatches);
        Assert.Equal(TwoStatements, script.Batches.Count);
        var first = script.Batches[0];
        Assert.IsType<SelectStatement>(Assert.Single(first.Statements));
        var firstSeparator = first.Separator;
        Assert.NotNull(firstSeparator);
        Assert.Equal("GO", SpanText(TwoBatches, firstSeparator.GoKeyword.Span));
        Assert.Equal(SyntaxKind.BatchSeparator, firstSeparator.GoKeyword.Kind);
        Assert.Null(firstSeparator.Count);
        var second = script.Batches[1].Separator;
        Assert.NotNull(second);
        Assert.Equal("3", SpanText(TwoBatches, Assert.NotNull(second.Count).Span));
        Assert.Equal("go 3", SpanText(TwoBatches, second.Span));
        Assert.Equal("select 1\nGO", SpanText(TwoBatches, first.Span));
        Assert.Equal(TwoBatches.TrimEnd()[..^"  -- run three times".Length], SpanText(TwoBatches, script.Span));
        SqlAssert.ParseFails(TwoBatches);
    }

    [Fact]
    public void Statements_after_the_last_go_form_a_final_batch()
    {
        const string sql = "select 1\r\ngo\r\nselect 2\r\nselect 3";
        var script = SqlAssert.ParseTsql<TsqlScript>(sql);
        var last = script.Batches[^1];
        Assert.Equal(TwoStatements, last.Statements.Count);
        Assert.Null(last.Separator);
    }

    [Fact]
    public void Empty_batches_keep_their_separators()
    {
        const string sql = "GO\n\nGO 2";
        var script = SqlAssert.ParseTsql<TsqlScript>(sql);
        Assert.Equal(TwoStatements, script.Batches.Count);
        Assert.All(script.Batches, batch => Assert.Empty(batch.Statements));
        Assert.Equal(sql, SpanText(sql, script.Span));
    }

    [Theory]
    [InlineData("select 1 go")]
    [InlineData("select 1 as go")]
    [InlineData("select 1, 'GO' from t -- GO")]
    [InlineData("select 1 /*\nGO\n*/")]
    public void Go_elsewhere_is_not_a_separator(string sql)
    {
        Assert.IsType<SelectStatement>(Sql.Parse(sql, SqlOptions.SqlServer).Root);
        Assert.DoesNotContain(SyntaxKind.BatchSeparator, Kinds(sql));
    }

    [Fact]
    public void Go_mid_line_is_an_identifier()
    {
        var column = Assert.IsType<IdentifierExpression>(SqlAssert.SelectTsql(SelectGo).SelectList[0].Expression);
        Assert.Equal("go", SpanText(SelectGo, column.Span));
    }

    [Theory]
    [InlineData("select 1\nGO x")]
    [InlineData("select 1\nGO 1 2")]
    public void Go_line_with_other_text_is_not_a_separator(string sql)
    {
        Assert.DoesNotContain(SyntaxKind.BatchSeparator, Kinds(sql));
        SqlAssert.ParseFails(sql, SqlOptions.SqlServer);
    }

    [Fact]
    public void Go_separator_accepts_a_trailing_comment()
    {
        Assert.Contains(SyntaxKind.BatchSeparator, Kinds("select 1\n\tgo -- next\nselect 2"));
        Assert.Contains(SyntaxKind.BatchSeparator, Kinds("select 1\ngo /* next */\nselect 2"));
    }

    [Fact]
    public void With_needs_a_terminated_predecessor()
    {
        Assert.Contains(SemicolonText, ParseError("select 1 with c as (select 1 as x) select x from c"), StringComparison.Ordinal);
        Assert.Equal(TwoStatements, SqlAssert.ParseTsql<TsqlBatch>(TerminatedWith).Statements.Count);
        SqlAssert.ParseTsql<WithQuery>("with c as (select 1 as x) select x from c");
    }

    [Fact]
    public void Throw_needs_a_terminated_predecessor()
    {
        Assert.Contains(SemicolonText, ParseError("update t set a = 1 throw"), StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_needs_a_terminator()
    {
        Assert.Contains("MERGE", ParseError("merge into t using s on t.id = s.id when matched then delete"), StringComparison.Ordinal);
        SqlAssert.ParseTsql<MergeStatement>(TerminatedMerge);
    }

    [Theory]
    [InlineData("")]
    [InlineData(Semicolon)]
    [InlineData("select 1 )")]
    [InlineData("1")]
    public void Rejects_inputs_without_a_statement(string sql)
    {
        SqlAssert.ParseFails(sql, SqlOptions.SqlServer);
    }

    [Fact]
    public void Visitor_walks_scripts_and_batches()
    {
        var visitor = new SqlVisitor();
        visitor.Visit(SqlAssert.ParseTsql<TsqlScript>(TwoBatches));
        visitor.Visit(SqlAssert.ParseTsql<TsqlBatch>(Terminated));
        Assert.NotNull(visitor);
    }
}
