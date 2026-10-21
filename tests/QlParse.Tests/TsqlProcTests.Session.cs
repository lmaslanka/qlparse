namespace QlParse.Tests;

// SQL Server transactions and session (stream E): BEGIN [DISTRIBUTED] / SAVE / COMMIT / ROLLBACK TRAN[SACTION],
// SET session and diagnostics options, SET TRANSACTION ISOLATION LEVEL, SET IDENTITY_INSERT and USE.
public sealed partial class TsqlProcTests
{
    [Fact]
    public void Parses_begin_transaction_with_name_and_mark()
    {
        const string sql = "begin tran t1 with mark 'nightly load'";
        var statement = SqlAssert.ParseTsql<TsqlTransactionStatement>(sql);
        Assert.Equal("begin", Text(sql, statement.VerbKeyword));
        Assert.Equal("tran", Text(sql, Assert.NotNull(statement.TransactionKeyword)));
        Assert.Equal("t1", Text(sql, Assert.NotNull(statement.Name)));
        Assert.Equal("mark", Text(sql, Assert.NotNull(statement.MarkKeyword)));
        Assert.Equal("'nightly load'", Text(sql, Assert.NotNull(statement.MarkDescription)));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_transaction_statements_in_a_batch()
    {
        const string sql = "begin transaction @tn save tran sp1 rollback transaction sp1 commit tran @tn begin distributed tran commit work rollback work rollback commit";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(Nine, batch.Statements.Count);
        var statements = batch.Statements.Cast<TsqlTransactionStatement>().ToList();
        Assert.Equal("@tn", Text(sql, Assert.NotNull(statements[0].Name)));
        Assert.Equal(SavepointName, Text(sql, Assert.NotNull(statements[1].Name)));
        Assert.Equal(SavepointName, Text(sql, Assert.NotNull(statements[Two].Name)));
        Assert.Equal("distributed", Text(sql, Assert.NotNull(statements[Four].DistributedKeyword)));
        Assert.Equal("work", Text(sql, Assert.NotNull(statements[Five].TransactionKeyword)));
        Assert.Null(statements[Seven].TransactionKeyword);
        Assert.Null(statements[Eight].Name);
    }

    [Fact]
    public void Transaction_name_never_takes_the_next_statement()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("begin tran insert into t values (1) commit tran print 'done'");
        Assert.Null(Assert.IsType<TsqlTransactionStatement>(batch.Statements[0]).Name);
        Assert.Null(Assert.IsType<TsqlTransactionStatement>(batch.Statements[Two]).Name);
        Assert.IsType<TsqlPrintStatement>(batch.Statements[Three]);
    }

    [Fact]
    public void Parses_commit_with_delayed_durability()
    {
        const string sql = "commit transaction t1 with (delayed_durability = on)";
        var statement = SqlAssert.ParseTsql<TsqlTransactionStatement>(sql);
        var durability = Present(statement.Durability);
        Assert.Equal(["delayed_durability"], Texts(sql, durability.Name));
        Assert.Equal(["on"], Texts(sql, durability.Value));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Transaction_statements_reject_bad_forms()
    {
        SqlAssert.ParseFails("save tran", SqlOptions.SqlServer);
        SqlAssert.ParseFails("save sp1", SqlOptions.SqlServer);
        SqlAssert.ParseFails("begin tran with mark_x", SqlOptions.SqlServer);
        FailsInIso("begin tran t1");
        FailsInIso("commit tran t1");
        SqlAssert.Parse<CommitStatement>("commit work");
    }

    [Fact]
    public void Parses_session_options()
    {
        const string sql = "set nocount on set ansi_nulls, quoted_identifier off set xact_abort on set datefirst 7 set dateformat dmy "
            + "set language us_english set lock_timeout -1 set deadlock_priority high set rowcount @n set textsize 2048 "
            + "set implicit_transactions off set context_info 0x01 set arithabort on set ansi_padding on set concat_null_yields_null on";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var statements = batch.Statements.Cast<TsqlSetOptionStatement>().ToList();
        Assert.Equal(Fifteen, statements.Count);
        Assert.Equal(OnWord, Text(sql, Assert.NotNull(statements[0].Toggle)));
        Assert.Equal(["ansi_nulls", "quoted_identifier"], Texts(sql, statements[1].Options));
        Assert.Equal(OffWord, Text(sql, Assert.NotNull(statements[1].Toggle)));
        Assert.Equal("7", Text(sql, Present(statements[Three].Value).Span));
        Assert.IsType<IdentifierExpression>(statements[Four].Value);
        Assert.IsType<UnaryExpression>(statements[Six].Value);
        Assert.IsType<EmbeddedHostExpression>(statements[Eight].Value);
        Assert.Null(statements[Eight].Toggle);
        Assert.Equal(sql, Text(sql, batch.Span));
    }

    [Fact]
    public void Parses_diagnostics_options()
    {
        const string sql = "set statistics io, time on set statistics xml off set statistics profile on set showplan_xml on "
            + "set showplan_text off set showplan_all on set noexec on set parseonly off";
        var statements = SqlAssert.ParseTsql<TsqlBatch>(sql).Statements.Cast<TsqlSetOptionStatement>().ToList();
        Assert.Equal(Eight, statements.Count);
        Assert.Equal("statistics", Text(sql, Assert.NotNull(statements[0].StatisticsKeyword)));
        Assert.Equal(["io", "time"], Texts(sql, statements[0].Options));
        Assert.Equal(["profile"], Texts(sql, statements[Two].Options));
        Assert.Null(statements[Three].StatisticsKeyword);
        Assert.Equal(["parseonly"], Texts(sql, statements[Seven].Options));
    }

    [Fact]
    public void Session_options_need_the_flag()
    {
        FailsInIso("set nocount on");
        FailsInIso("set statistics io on");
        SqlAssert.ParseFails("set nocount", SqlOptions.SqlServer);
    }

    [Theory]
    [InlineData("set transaction isolation level read uncommitted", "read uncommitted")]
    [InlineData("set transaction isolation level read committed", "read committed")]
    [InlineData("set transaction isolation level repeatable read", "repeatable read")]
    [InlineData("set transaction isolation level snapshot", "snapshot")]
    [InlineData("set transaction isolation level serializable", "serializable")]
    public void Parses_isolation_levels(string sql, string level)
    {
        var statement = SqlAssert.ParseTsql<TsqlSetTransactionIsolationLevelStatement>(sql);
        Assert.Equal(level, string.Join(' ', Texts(sql, statement.Level)));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Snapshot_isolation_needs_the_flag()
    {
        FailsInIso("set transaction isolation level snapshot");
        SqlAssert.ParseFails("set transaction isolation level fast", SqlOptions.SqlServer);
        SqlAssert.Parse<SetTransactionStatement>("set transaction isolation level serializable");
    }

    [Fact]
    public void Parses_identity_insert()
    {
        const string sql = "set identity_insert dbo.orders on set identity_insert #t off";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var on = Assert.IsType<TsqlSetIdentityInsertStatement>(batch.Statements[0]);
        Assert.Equal(["dbo", "orders"], Texts(sql, on.Table));
        Assert.Equal(OnWord, Text(sql, on.Toggle));
        var off = Assert.IsType<TsqlSetIdentityInsertStatement>(batch.Statements[1]);
        Assert.Equal(OffWord, Text(sql, off.Toggle));
        SqlAssert.ParseFails("set identity_insert t", SqlOptions.SqlServer);
        FailsInIso("set identity_insert t on");
    }

    [Fact]
    public void Parses_use()
    {
        const string sql = "use [AdventureWorks] use master";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal("[AdventureWorks]", Text(sql, Assert.IsType<TsqlUseStatement>(batch.Statements[0]).Database));
        Assert.Equal("master", Text(sql, Assert.IsType<TsqlUseStatement>(batch.Statements[1]).Database));
        FailsInIso("use master");
    }

    [Fact]
    public void Visitor_walks_every_procedural_node()
    {
        const string sql = "create procedure p @a int = 1 with recompile as begin declare @t table (id int), @c cursor, @n int = 0 "
            + "set @n += 1 set @c = cursor for select id from @t if @n > 0 print 'x' else while @n < 5 begin set @n = @n + 1 break end "
            + "begin try exec q @a, @b = default with result sets ((x int)) end try begin catch throw end catch "
            + "raiserror ('m', 1, 1) waitfor delay '00:00:01' goto done done: return @n end\ngo\n"
            + "create function f (@x int) returns @r table (v int) as begin insert into @r values (@x) return end\ngo\n"
            + "create function g () returns int with native_compilation as begin atomic with (language = N'English') return 1 end\ngo\n"
            + "create trigger tr on t after update as if update(a) rollback\ngo\n"
            + "create or alter view v as select 1 as a\ngo\n"
            + "create aggregate ag (@v int) returns int external name a.b\ngo\n"
            + "declare c cursor local for select 1 open c fetch next from c close c deallocate c continue\n"
            + "exec ('select 1') set nocount on set transaction isolation level snapshot set identity_insert t on use db\n"
            + "begin tran commit tran with (delayed_durability = on) enable trigger all on database\n"
            + "create event notification n on server for logon to service 's', 'current database'";
        var counter = new CountingVisitor();
        counter.Visit(SqlAssert.ParseTsql<TsqlScript>(sql));
        Assert.True(counter.Statements > 40);
        Assert.True(counter.Expressions > 20);
    }

    private sealed class CountingVisitor : SqlVisitor
    {
        public int Statements { get; private set; }

        public int Expressions { get; private set; }

        public override void Visit(Query query)
        {
            Statements++;
            base.Visit(query);
        }

        public override void Visit(Expression expression)
        {
            Expressions++;
            base.Visit(expression);
        }
    }
}
