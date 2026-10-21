namespace QlParse.Tests;

// SQL Server modules (stream E): CREATE [OR ALTER] / ALTER PROCEDURE, scalar, inline and multi-statement
// table-valued functions, natively compiled modules, CLR modules (EXTERNAL NAME), CREATE AGGREGATE and
// CREATE OR ALTER VIEW.
public sealed partial class TsqlProcTests
{
    private const string NumberedProcedureDefinition = "create procedure p"
        + Semicolon
        + "2 as select 2"
        ;

    // ---- Procedures ----

    [Fact]
    public void Parses_create_procedure_with_parameters_options_and_body()
    {
        const string sql = "create procedure dbo.usp_get @id int, @name nvarchar(50) = N'x' output, @rows int = null out "
            + "with recompile, encryption, execute as owner as set nocount on select @name = name from t where id = @id return 0";
        var statement = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        Assert.Equal("create", Text(sql, Assert.NotNull(statement.CreateKeyword)));
        Assert.Null(statement.AlterKeyword);
        Assert.Equal(["dbo", "usp_get"], Texts(sql, statement.Name));
        Assert.Null(statement.OpenParen);
        Assert.Equal(Three, statement.Parameters.Count);
        var name = statement.Parameters[1];
        Assert.Equal("nvarchar", Text(sql, Present(name.DataType).Name));
        Assert.Equal("N'x'", Text(sql, Present(name.Default).Span));
        Assert.Equal("output", Text(sql, Assert.NotNull(name.OutputKeyword)));
        Assert.Equal("out", Text(sql, Assert.NotNull(statement.Parameters[Two].OutputKeyword)));
        Assert.Equal(Three, statement.Options.Count);
        Assert.Equal(["execute", "as"], Texts(sql, statement.Options[Two].Name));
        Assert.Equal(["owner"], Texts(sql, statement.Options[Two].Value));
        Assert.Equal(Three, statement.Statements.Count);
        Assert.IsType<TsqlReturnStatement>(statement.Statements[Two]);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_parenthesized_parameters_and_readonly_table_parameter()
    {
        const string sql = "create proc p (@t dbo.IdList readonly, @c cursor varying output) as begin select * from @t end";
        var statement = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        Assert.Equal("proc", Text(sql, statement.ProcedureKeyword));
        Assert.NotNull(statement.OpenParen);
        Assert.Equal("readonly", Text(sql, Assert.NotNull(statement.Parameters[0].ReadonlyKeyword)));
        var cursor = statement.Parameters[1];
        Assert.Equal("cursor", Text(sql, Assert.NotNull(cursor.CursorKeyword)));
        Assert.Null(cursor.DataType);
        Assert.NotNull(cursor.VaryingKeyword);
        Assert.IsType<TsqlBlockStatement>(Assert.Single(statement.Statements));
    }

    [Fact]
    public void Procedure_body_runs_to_the_end_of_the_batch()
    {
        const string sql = "create procedure p as select 1 select 2\ngo\nexec p";
        var script = SqlAssert.ParseTsql<TsqlScript>(sql);
        var procedure = Assert.IsType<TsqlCreateProcedureStatement>(Assert.Single(script.Batches[0].Statements));
        Assert.Equal(Two, procedure.Statements.Count);
        Assert.IsType<TsqlExecuteStatement>(Assert.Single(script.Batches[1].Statements));
    }

    [Fact]
    public void Parses_create_or_alter_and_alter_procedure()
    {
        const string sql = "create or alter procedure p @a int as print @a";
        var orAlter = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        Assert.Equal(OrWord, Text(sql, Assert.NotNull(orAlter.OrKeyword)));
        Assert.Equal(AlterWord, Text(sql, Assert.NotNull(orAlter.AlterKeyword)));
        const string alterSql = "alter proc p as print 1";
        var alter = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(alterSql);
        Assert.Null(alter.CreateKeyword);
        Assert.Equal(alterSql, Text(alterSql, alter.Span));
    }

    [Fact]
    public void Parses_numbered_procedure_and_for_replication()
    {
        var numbered = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(NumberedProcedureDefinition);
        Assert.Equal("2", Text(NumberedProcedureDefinition, Assert.NotNull(numbered.Number)));
        const string sql = "create procedure p with encryption for replication as select 1";
        var replication = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        Assert.Equal("replication", Text(sql, Assert.NotNull(replication.ReplicationKeyword)));
    }

    [Fact]
    public void Procedure_needs_a_body_and_the_flag()
    {
        SqlAssert.ParseFails("create procedure p as", SqlOptions.SqlServer);
        SqlAssert.ParseFails("create procedure p @a int", SqlOptions.SqlServer);
        FailsInIso("create procedure p @a int as select 1");
        FailsInIso("create or alter procedure p as select 1");
    }

    [Fact]
    public void Parses_clr_procedure()
    {
        const string sql = "create procedure dbo.clr_proc @x int as external name [Asm].[Ns.Procs].[Run]";
        var statement = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        var external = Present(statement.ExternalName);
        Assert.Equal(["[Asm]", "[Ns.Procs]", "[Run]"], Texts(sql, external.Name));
        Assert.Empty(statement.Statements);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    // ---- Functions ----

    [Fact]
    public void Parses_scalar_function()
    {
        const string sql = "create function dbo.f (@a int, @b int = 2) returns int with schemabinding, returns null on null input, inline = on "
            + "as begin declare @r int = @a * @b return @r end";
        var statement = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        Assert.Equal(Two, statement.Parameters.Count);
        Assert.Equal("int", Text(sql, Present(statement.ReturnType).Name));
        Assert.Equal(Three, statement.Options.Count);
        Assert.Equal(["returns", "null", "on", "null", "input"], Texts(sql, statement.Options[1].Name));
        Assert.Equal(["on"], Texts(sql, statement.Options[Two].Value));
        var body = Assert.IsType<TsqlBlockStatement>(statement.Body);
        Assert.IsType<TsqlReturnStatement>(body.Statements[1]);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_called_on_null_input_and_function_without_as()
    {
        const string sql = "create function f () returns bit with called on null input begin return 1 end";
        var statement = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        Assert.Empty(statement.Parameters);
        Assert.Null(statement.AsKeyword);
        Assert.Equal(["called", "on", "null", "input"], Texts(sql, Assert.Single(statement.Options).Name));
    }

    [Fact]
    public void Parses_inline_table_valued_function()
    {
        const string sql = "create or alter function dbo.tvf (@id int) returns table as return (select a from t where id = @id)";
        var statement = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        Assert.Equal("table", Text(sql, Assert.NotNull(statement.ReturnTableKeyword)));
        var body = Assert.IsType<TsqlReturnStatement>(statement.Body);
        Assert.IsType<ParenQuery>(body.Query);
        Assert.Null(body.Value);
        var bare = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>("create function f () returns table return select 1 as a");
        Assert.IsType<SelectStatement>(Assert.IsType<TsqlReturnStatement>(bare.Body).Query);
    }

    [Fact]
    public void Parses_multi_statement_table_valued_function()
    {
        const string sql = "create function f (@n int) returns @r table (id int primary key, v int) as begin insert into @r values (@n, 1) return end";
        var statement = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        Assert.Equal("@r", Text(sql, Assert.NotNull(statement.ReturnVariable)));
        Assert.Equal(Two, Present(statement.ReturnTable).Elements.Count);
        var body = Assert.IsType<TsqlBlockStatement>(statement.Body);
        Assert.IsType<InsertStatement>(body.Statements[0]);
        Assert.Null(Assert.IsType<TsqlReturnStatement>(body.Statements[1]).Value);
    }

    [Fact]
    public void Parses_clr_functions()
    {
        const string sql = "create function f (@s nvarchar(100)) returns table (part nvarchar(100)) external name a.b.c";
        var table = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        Assert.Null(table.ReturnVariable);
        Assert.Single(Present(table.ReturnTable).Elements);
        Assert.Equal(["a", "b", "c"], Texts(sql, Present(table.ExternalName).Name));
        Assert.Null(table.Body);
        var scalar = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>("create function f (@a int) returns int as external name a.b.c");
        Assert.NotNull(scalar.ExternalName);
    }

    [Fact]
    public void Function_needs_a_body_and_the_flag()
    {
        SqlAssert.ParseFails("create function f () returns int as select 1", SqlOptions.SqlServer);
        SqlAssert.ParseFails("create function f returns int as begin return 1 end", SqlOptions.SqlServer);
        FailsInIso("create function f (@a int) returns int as begin return @a end");
    }

    // ---- Natively compiled modules ----

    [Fact]
    public void Parses_natively_compiled_procedure()
    {
        const string sql = "create procedure dbo.native @id int not null with native_compilation, schemabinding, execute as owner as "
            + "begin atomic with (transaction isolation level = snapshot, language = N'us_english', delayed_durability = on) select @id end";
        var statement = SqlAssert.ParseTsql<TsqlCreateProcedureStatement>(sql);
        Assert.NotNull(statement.Parameters[0].NotKeyword);
        Assert.Equal(["native_compilation"], Texts(sql, statement.Options[0].Name));
        var atomic = Assert.IsType<TsqlAtomicBlockStatement>(Assert.Single(statement.Statements));
        Assert.Equal(Three, atomic.Options.Count);
        Assert.Equal(["transaction", "isolation", "level"], Texts(sql, atomic.Options[0].Name));
        Assert.Equal(["snapshot"], Texts(sql, atomic.Options[0].Value));
        Assert.Equal(["N'us_english'"], Texts(sql, atomic.Options[1].Value));
        Assert.Single(atomic.Statements);
    }

    [Fact]
    public void Parses_natively_compiled_function_with_repeatable_read()
    {
        const string sql = "create function f (@a int) returns int with native_compilation, schemabinding as "
            + "begin atomic with (transaction isolation level = repeatable read, language = N'English') return @a end";
        var statement = SqlAssert.ParseTsql<TsqlCreateFunctionStatement>(sql);
        var atomic = Assert.IsType<TsqlAtomicBlockStatement>(statement.Body);
        Assert.Equal(["repeatable", "read"], Texts(sql, atomic.Options[0].Value));
        SqlAssert.ParseFails("begin atomic select 1 end", SqlOptions.SqlServer);
    }

    // ---- CREATE AGGREGATE, CREATE OR ALTER VIEW ----

    [Fact]
    public void Parses_clr_aggregate()
    {
        const string sql = "create aggregate dbo.Concat (@value nvarchar(4000), @sep nvarchar(10)) returns nvarchar(4000) external name StringUtil.[Concatenate]";
        var statement = SqlAssert.ParseTsql<TsqlCreateAggregateStatement>(sql);
        Assert.Equal(["dbo", "Concat"], Texts(sql, statement.Name));
        Assert.Equal(Two, statement.Parameters.Count);
        Assert.Equal(["StringUtil", "[Concatenate]"], Texts(sql, statement.ExternalName.Name));
        Assert.Equal(sql, Text(sql, statement.Span));
        SqlAssert.ParseFails("create aggregate a (@v int) returns int", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_create_or_alter_view()
    {
        const string sql = "create or alter view dbo.v (a, b) as select a, b from t";
        var statement = SqlAssert.ParseTsql<TsqlCreateOrAlterViewStatement>(sql);
        Assert.Equal(OrWord, Text(sql, statement.OrKeyword));
        Assert.Equal(AlterWord, Text(sql, statement.View.CreateKeyword));
        Assert.Equal(["dbo", "v"], Texts(sql, statement.View.Name));
        Assert.Equal(["a", "b"], Texts(sql, Present(statement.View.Columns)));
        Assert.Equal(sql, Text(sql, statement.Span));
        SqlAssert.ParseTsql<CreateViewStatement>("create view v as select 1 as a");
        FailsInIso("create or alter view v as select 1 as a");
    }
}
