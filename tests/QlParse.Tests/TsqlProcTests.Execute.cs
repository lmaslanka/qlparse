namespace QlParse.Tests;

// SQL Server EXEC[UTE] (stream E): procedure calls with named, OUTPUT and DEFAULT arguments, return status,
// numbered procedures, @module variables, WITH RECOMPILE / RESULT SETS, dynamic SQL strings, sp_executesql,
// system procedures and INSERT ... EXEC.
public sealed partial class TsqlProcTests
{
    private const string NumberedProcedure = "exec dbo.p"
        + Semicolon
        + "2 @a = 1"
        ;

    [Fact]
    public void Parses_execute_with_return_status_and_arguments()
    {
        const string sql = "exec @rc = dbo.usp_get 1, @name = N'x', @out = @v output, @flag = default with recompile";
        var statement = SqlAssert.ParseTsql<TsqlExecuteStatement>(sql);
        Assert.Equal("@rc", Text(sql, Assert.NotNull(statement.ReturnStatus)));
        Assert.Equal(["dbo", "usp_get"], Texts(sql, statement.Module));
        Assert.Equal(4, statement.Arguments.Count);
        Assert.Null(statement.Arguments[0].Parameter);
        Assert.Equal("1", Text(sql, Present(statement.Arguments[0].Value).Span));
        Assert.Equal("@name", Text(sql, Assert.NotNull(statement.Arguments[1].Parameter)));
        Assert.Equal("output", Text(sql, Assert.NotNull(statement.Arguments[2].OutputKeyword)));
        Assert.Equal("default", Text(sql, Assert.NotNull(statement.Arguments[3].DefaultKeyword)));
        Assert.Null(statement.Arguments[3].Value);
        Assert.Equal(["recompile"], Texts(sql, Assert.Single(statement.Options).Keywords));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_execute_without_arguments_before_next_statement()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("execute p select 1 exec q print 'x' exec r");
        Assert.Empty(Assert.IsType<TsqlExecuteStatement>(batch.Statements[0]).Arguments);
        Assert.Empty(Assert.IsType<TsqlExecuteStatement>(batch.Statements[2]).Arguments);
        Assert.Equal(5, batch.Statements.Count);
    }

    [Fact]
    public void Parses_bare_identifier_and_out_arguments()
    {
        const string sql = "exec sp_help t1 exec p @x out";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var help = Assert.IsType<TsqlExecuteStatement>(batch.Statements[0]);
        Assert.IsType<IdentifierExpression>(Assert.Single(help.Arguments).Value);
        var out1 = Assert.IsType<TsqlExecuteStatement>(batch.Statements[1]);
        Assert.Equal("out", Text(sql, Assert.NotNull(Assert.Single(out1.Arguments).OutputKeyword)));
    }

    [Fact]
    public void Parses_numbered_procedure_and_module_variable()
    {
        var numbered = SqlAssert.ParseTsql<TsqlExecuteStatement>(NumberedProcedure);
        Assert.Equal("2", Text(NumberedProcedure, Assert.NotNull(numbered.Number)));
        Assert.Single(numbered.Arguments);
        const string sql = "exec @procname @a = 1";
        var variable = SqlAssert.ParseTsql<TsqlExecuteStatement>(sql);
        Assert.Equal(["@procname"], Texts(sql, variable.Module));
        Assert.Null(variable.ReturnStatus);
    }

    [Fact]
    public void Parses_sp_executesql()
    {
        const string sql = "exec sp_executesql @stmt, N'@p int, @q nvarchar(10) output', @p = 1, @q = @r output";
        var statement = SqlAssert.ParseTsql<TsqlExecuteStatement>(sql);
        Assert.Equal(["sp_executesql"], Texts(sql, statement.Module));
        Assert.Equal(4, statement.Arguments.Count);
        Assert.NotNull(statement.Arguments[3].OutputKeyword);
    }

    [Theory]
    [InlineData("exec sp_rename 'dbo.t.old', 'new', 'COLUMN'", "sp_rename")]
    [InlineData("exec sp_sequence_get_range @sequence_name = N'dbo.s', @range_size = 10, @range_first_value = @first output", "sp_sequence_get_range")]
    [InlineData("exec sp_bindefault 'df_zero', 't.c'", "sp_bindefault")]
    [InlineData("exec sp_bindrule 'r_positive', 't.c'", "sp_bindrule")]
    [InlineData("exec xp_cmdshell 'dir'", "xp_cmdshell")]
    [InlineData("execute sp_who", "sp_who")]
    public void Parses_system_procedures_without_schema(string sql, string procedure)
    {
        var statement = SqlAssert.ParseTsql<TsqlExecuteStatement>(sql);
        Assert.Equal([procedure], Texts(sql, statement.Module));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_result_sets_options()
    {
        const string sql = "exec p with recompile, result sets ((id int not null, name varchar(10) collate latin1_general_ci_as null), as object dbo.t, as type dbo.tt, as for xml)";
        var statement = SqlAssert.ParseTsql<TsqlExecuteStatement>(sql);
        Assert.Equal(2, statement.Options.Count);
        var resultSets = statement.Options[1];
        Assert.Equal(["result", "sets"], Texts(sql, resultSets.Keywords));
        Assert.Equal(4, resultSets.ResultSets.Count);
        var columns = resultSets.ResultSets[0].Columns;
        Assert.NotNull(columns[0].NotKeyword);
        Assert.Equal("latin1_general_ci_as", Text(sql, Assert.NotNull(columns[1].Collation)));
        Assert.Equal(["object"], Texts(sql, resultSets.ResultSets[1].Kind));
        Assert.Equal(["dbo", "tt"], Texts(sql, resultSets.ResultSets[2].Name));
        Assert.Equal(["for", "xml"], Texts(sql, resultSets.ResultSets[3].Kind));
    }

    [Theory]
    [InlineData("exec p with result sets none", "none")]
    [InlineData("exec p with result sets undefined", "undefined")]
    public void Parses_result_sets_none_and_undefined(string sql, string kind)
    {
        var option = Assert.Single(SqlAssert.ParseTsql<TsqlExecuteStatement>(sql).Options);
        Assert.Equal(kind, Text(sql, option.Keywords[^1]));
    }

    [Fact]
    public void Parses_execute_string()
    {
        const string sql = "exec (N'select * from ' + @table) as login = 'app'";
        var statement = SqlAssert.ParseTsql<TsqlExecuteStringStatement>(sql);
        Assert.IsType<BinaryExpression>(statement.Command);
        Assert.Equal("login", Text(sql, Assert.NotNull(statement.PrincipalKind)));
        Assert.Equal("'app'", Text(sql, Assert.NotNull(statement.Principal)));
        Assert.Null(statement.AtKeyword);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_execute_string_at_linked_server_and_data_source()
    {
        const string sql = "execute ('select ?, ?', 1, @b) as user = 'u' at remote_srv";
        var statement = SqlAssert.ParseTsql<TsqlExecuteStringStatement>(sql);
        Assert.Equal(2, statement.Arguments.Count);
        Assert.Equal("user", Text(sql, Assert.NotNull(statement.PrincipalKind)));
        Assert.Equal("remote_srv", Text(sql, Assert.NotNull(statement.Target)));
        const string dataSource = "exec ('select 1') at data_source ds";
        var external = SqlAssert.ParseTsql<TsqlExecuteStringStatement>(dataSource);
        Assert.Equal("data_source", Text(dataSource, Assert.NotNull(external.DataSourceKeyword)));
        Assert.Equal("ds", Text(dataSource, Assert.NotNull(external.Target)));
    }

    [Fact]
    public void Execute_as_is_not_a_procedure_call()
    {
        SqlAssert.ParseFails("execute as user = 'x'", SqlOptions.SqlServer);
        SqlAssert.ParseFails("exec as login = 'x'", SqlOptions.SqlServer);
    }

    [Fact]
    public void Execute_requires_the_flag()
    {
        FailsInIso("exec p 1");
        FailsInIso("exec ('select 1')");
        SqlAssert.ParseFails("exec ('select 1'", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_insert_exec()
    {
        const string sql = "insert into t (a, b) exec dbo.p @x = 1";
        var insert = SqlAssert.ParseTsql<InsertStatement>(sql);
        Assert.Equal(["a", "b"], Texts(sql, Present(insert.Columns)));
        var exec = Assert.IsType<TsqlExecuteStatement>(insert.Query);
        Assert.Equal(["dbo", "p"], Texts(sql, exec.Module));
        Assert.Equal(sql, Text(sql, insert.Span));
        var dynamic = SqlAssert.ParseTsql<InsertStatement>("insert into @t execute ('select 1')");
        Assert.IsType<TsqlExecuteStringStatement>(dynamic.Query);
        Assert.IsType<SelectStatement>(SqlAssert.ParseTsql<InsertStatement>("insert into t select 1").Query);
        FailsInIso("insert into t exec p");
    }
}
