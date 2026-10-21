namespace QlParse.Tests;

// SQL Server procedural T-SQL (stream E): control flow. Variables and cursors are in TsqlProcTests.Declare.cs,
// EXEC in TsqlProcTests.Execute.cs, modules in TsqlProcTests.Module.cs, triggers in TsqlProcTests.Trigger.cs and
// transactions and session options in TsqlProcTests.Session.cs.
public sealed partial class TsqlProcTests
{
    private const int Two = 2;
    private const int Three = 3;
    private const int Four = 4;
    private const int Five = 5;
    private const int Six = 6;
    private const int Seven = 7;
    private const int Eight = 8;
    private const int Nine = 9;
    private const int Fifteen = 15;
    private const string Semicolon = ";";
    private const string VariableA = "@a";
    private const string CursorC = "c";
    private const string OrWord = "or";
    private const string AlterWord = "alter";
    private const string SavepointName = "sp1";
    private const string OnWord = "on";
    private const string OffWord = "off";
    private const string DatabaseWord = "database";
    private const string RetryLabel = "retry";
    private const string CatchWord = "catch";
    private const string ThrowInCatch = "begin try select 1 end try begin catch print 'failed'"
        + Semicolon
        + " throw end catch"
        ;
    private const string IfTerminatedBeforeElse = "if @a = 1 print 'one'"
        + Semicolon
        + " else print 'other'"
        ;
    private const string IfTerminatedWithoutElse = "if @a = 1 print 'one'"
        + Semicolon
        + " print 'next'"
        ;
    private const string ThrowAfterTerminator = "print 1"
        + Semicolon
        + " throw 50000, 'boom', 1"
        ;

    private static string Text(string sql, SyntaxToken token) => sql[token.Position..token.Span.End];

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string[] Texts(string sql, IEnumerable<SyntaxToken> tokens) => [.. tokens.Select(token => Text(sql, token))];

    private static void FailsInIso(string sql) => SqlAssert.ParseFails(sql);

    private static T Present<T>(T? value) where T : class
    {
        Assert.NotNull(value);
        return value;
    }

    // ---- BEGIN ... END ----

    [Fact]
    public void Parses_begin_end_block_with_several_statements()
    {
        const string sql = "begin print 'a' select 1 end";
        var block = SqlAssert.ParseTsql<TsqlBlockStatement>(sql);
        Assert.Equal("begin", Text(sql, block.BeginKeyword));
        Assert.Collection(
            block.Statements,
            statement => Assert.IsType<TsqlPrintStatement>(statement),
            statement => Assert.IsType<SelectStatement>(statement));
        Assert.Equal("end", Text(sql, block.EndKeyword));
        Assert.Equal(sql, Text(sql, block.Span));
    }

    [Fact]
    public void Parses_nested_and_empty_blocks()
    {
        var block = SqlAssert.ParseTsql<TsqlBlockStatement>("begin begin end begin select 1 end end");
        Assert.Equal(Two, block.Statements.Count);
        Assert.Empty(Assert.IsType<TsqlBlockStatement>(block.Statements[0]).Statements);
        Assert.Single(Assert.IsType<TsqlBlockStatement>(block.Statements[1]).Statements);
    }

    [Fact]
    public void Block_requires_end()
    {
        SqlAssert.ParseFails("begin select 1", SqlOptions.SqlServer);
        FailsInIso("begin print 'a' end");
    }

    // ---- IF / ELSE ----

    [Fact]
    public void Parses_if_with_single_statement_branches()
    {
        const string sql = "if @a > 1 print 'big' else select 2";
        var statement = SqlAssert.ParseTsql<TsqlIfStatement>(sql);
        Assert.Equal("@a > 1", Text(sql, statement.Condition.Span));
        Assert.IsType<TsqlPrintStatement>(statement.Then);
        Assert.Equal("else", Text(sql, Assert.NotNull(statement.ElseKeyword)));
        Assert.IsType<SelectStatement>(statement.Else);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_if_with_blocks_and_else_if_chain()
    {
        const string sql = "if exists (select 1 from t) begin print 'a' print 'b' end else if @x is null return else begin select 3 end";
        var statement = SqlAssert.ParseTsql<TsqlIfStatement>(sql);
        Assert.IsType<ExistsExpression>(statement.Condition);
        Assert.Equal(Two, Assert.IsType<TsqlBlockStatement>(statement.Then).Statements.Count);
        var elseIf = Assert.IsType<TsqlIfStatement>(statement.Else);
        Assert.IsType<TsqlReturnStatement>(elseIf.Then);
        Assert.IsType<TsqlBlockStatement>(elseIf.Else);
    }

    [Fact]
    public void Else_binds_to_the_nearest_if()
    {
        var outer = SqlAssert.ParseTsql<TsqlIfStatement>("if @a = 1 if @b = 2 print 'x' else print 'y'");
        Assert.Null(outer.Else);
        Assert.NotNull(Assert.IsType<TsqlIfStatement>(outer.Then).Else);
    }

    [Fact]
    public void Terminator_before_else_is_allowed()
    {
        var statement = SqlAssert.ParseTsql<TsqlIfStatement>(IfTerminatedBeforeElse);
        Assert.IsType<TsqlPrintStatement>(statement.Else);
        var batch = SqlAssert.ParseTsql<TsqlBatch>(IfTerminatedWithoutElse);
        Assert.Null(Assert.IsType<TsqlIfStatement>(batch.Statements[0]).Else);
        Assert.Single(batch.Semicolons);
    }

    [Fact]
    public void If_needs_a_statement_and_no_then()
    {
        SqlAssert.ParseFails("if @a = 1", SqlOptions.SqlServer);
        SqlAssert.ParseFails("if @a = 1 then print 1 end if", SqlOptions.SqlServer);
        FailsInIso("if 1 = 1 print 'x'");
    }

    // ---- WHILE / BREAK / CONTINUE ----

    [Fact]
    public void Parses_while_with_break_and_continue()
    {
        const string sql = "while @i < 10 begin set @i += 1 if @i = 5 continue if @i = 8 break end";
        var loop = SqlAssert.ParseTsql<TsqlWhileStatement>(sql);
        Assert.Equal("@i < 10", Text(sql, loop.Condition.Span));
        var body = Assert.IsType<TsqlBlockStatement>(loop.Body);
        Assert.Equal(Three, body.Statements.Count);
        Assert.IsType<TsqlContinueStatement>(Assert.IsType<TsqlIfStatement>(body.Statements[1]).Then);
        var breakStatement = Assert.IsType<TsqlBreakStatement>(Assert.IsType<TsqlIfStatement>(body.Statements[Two]).Then);
        Assert.Equal("break", Text(sql, breakStatement.BreakKeyword));
        Assert.Equal(sql, Text(sql, loop.Span));
    }

    [Fact]
    public void Parses_while_with_single_statement_body()
    {
        var loop = SqlAssert.ParseTsql<TsqlWhileStatement>("while (select count(*) from t) > 0 delete from t where id = 1");
        Assert.IsType<DeleteStatement>(loop.Body);
        FailsInIso("while 1 = 1 break");
    }

    // ---- GOTO and labels ----

    [Fact]
    public void Parses_goto_and_labels()
    {
        const string sql = "retry: print 1 goto retry";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var label = Assert.IsType<TsqlLabelStatement>(batch.Statements[0]);
        Assert.Equal(RetryLabel, Text(sql, label.Label));
        Assert.Equal(":", Text(sql, label.Colon));
        Assert.IsType<TsqlPrintStatement>(batch.Statements[1]);
        var jump = Assert.IsType<TsqlGotoStatement>(batch.Statements[Two]);
        Assert.Equal(RetryLabel, Text(sql, jump.Label));
        FailsInIso("goto retry");
    }

    [Fact]
    public void Label_does_not_take_the_next_statement()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("done: begin print 1 end");
        Assert.IsType<TsqlLabelStatement>(batch.Statements[0]);
        Assert.IsType<TsqlBlockStatement>(batch.Statements[1]);
        SqlAssert.Parse<CompoundStatement>("done: begin end");
    }

    // ---- RETURN ----

    [Fact]
    public void Parses_return_with_and_without_value()
    {
        const string sql = "return @@error * 2";
        var withValue = SqlAssert.ParseTsql<TsqlReturnStatement>(sql);
        Assert.IsType<BinaryExpression>(withValue.Value);
        Assert.Equal(sql, Text(sql, withValue.Span));
        var batch = SqlAssert.ParseTsql<TsqlBatch>("return select 1");
        Assert.Null(Assert.IsType<TsqlReturnStatement>(batch.Statements[0]).Value);
        Assert.IsType<SelectStatement>(batch.Statements[1]);
        var reserved = SqlAssert.ParseTsql<TsqlBatch>("if @a = 1 return print 'x'");
        Assert.Null(Assert.IsType<TsqlReturnStatement>(Assert.IsType<TsqlIfStatement>(reserved.Statements[0]).Then).Value);
        Assert.Null(SqlAssert.ParseTsql<TsqlReturnStatement>("return").Value);
    }

    // ---- WAITFOR ----

    [Fact]
    public void Parses_waitfor_delay_and_time()
    {
        const string sql = "waitfor delay '00:00:05' waitfor time @at";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var delay = Assert.IsType<TsqlWaitforStatement>(batch.Statements[0]);
        Assert.Equal("delay", Text(sql, Assert.NotNull(delay.DelayOrTimeKeyword)));
        Assert.Equal("'00:00:05'", Text(sql, Present(delay.Value).Span));
        var time = Assert.IsType<TsqlWaitforStatement>(batch.Statements[1]);
        Assert.Equal("time", Text(sql, Assert.NotNull(time.DelayOrTimeKeyword)));
        Assert.IsType<EmbeddedHostExpression>(time.Value);
        SqlAssert.ParseFails("waitfor '00:00:05'", SqlOptions.SqlServer);
        FailsInIso("waitfor delay '00:00:05'");
    }

    [Fact]
    public void Parses_waitfor_receive_with_timeout()
    {
        const string sql = "waitfor (receive top (1) conversation_handle, message_body from dbo.target_queue), timeout 5000";
        var statement = SqlAssert.ParseTsql<TsqlWaitforStatement>(sql);
        var receive = Assert.IsType<TsqlReceiveStatement>(statement.Statement);
        Assert.Equal(["dbo", "target_queue"], Texts(sql, receive.Queue));
        Assert.Equal("timeout", Text(sql, Assert.NotNull(statement.TimeoutKeyword)));
        Assert.Equal("5000", Text(sql, Present(statement.Timeout).Span));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_waitfor_get_conversation_group()
    {
        const string sql = "waitfor (get conversation group @g from dbo.q)";
        var statement = SqlAssert.ParseTsql<TsqlWaitforStatement>(sql);
        Assert.IsType<TsqlGetConversationGroupStatement>(statement.Statement);
        Assert.Null(statement.Timeout);
        Assert.Equal(sql, Text(sql, statement.Span));
        SqlAssert.ParseFails("waitfor (select 1)", SqlOptions.SqlServer);
        SqlAssert.ParseFails("waitfor (receive * from q), 10", SqlOptions.SqlServer);
    }

    [Fact]
    public void Service_broker_statements_are_not_blocks()
    {
        const string sql = "begin begin dialog @h from service s to service 't' on contract c send on conversation @h (N'x') end conversation @h end";
        var block = SqlAssert.ParseTsql<TsqlBlockStatement>(sql);
        Assert.Collection(
            block.Statements,
            statement => Assert.IsType<TsqlBeginDialogStatement>(statement),
            statement => Assert.IsType<TsqlSendStatement>(statement),
            statement => Assert.IsType<TsqlEndConversationStatement>(statement));
        Assert.Equal(sql, Text(sql, block.Span));
        SqlAssert.ParseTsql<TsqlBeginConversationTimerStatement>("begin conversation timer (@h) timeout = 60");
        var conversation = SqlAssert.ParseTsql<TsqlBlockStatement>("begin conversation: print 1 end");
        Assert.IsType<TsqlLabelStatement>(conversation.Statements[0]);
    }

    // ---- TRY / CATCH, THROW, RAISERROR, PRINT ----

    [Fact]
    public void Parses_try_catch()
    {
        const string sql = "begin try insert into t values (1) update t set a = 2 end try begin catch select error_message() end catch";
        var statement = SqlAssert.ParseTsql<TsqlTryCatchStatement>(sql);
        Assert.Equal(Two, statement.TryStatements.Count);
        Assert.Equal("try", Text(sql, statement.TryEndTryKeyword));
        Assert.Equal(CatchWord, Text(sql, statement.CatchKeyword));
        Assert.Single(statement.CatchStatements);
        Assert.Equal(CatchWord, Text(sql, statement.CatchEndCatchKeyword));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_rethrow_inside_catch_and_empty_catch()
    {
        var statement = SqlAssert.ParseTsql<TsqlTryCatchStatement>(ThrowInCatch);
        Assert.Empty(Assert.IsType<TsqlThrowStatement>(statement.CatchStatements[1]).Arguments);
        Assert.Empty(SqlAssert.ParseTsql<TsqlTryCatchStatement>("begin try print 1 end try begin catch end catch").CatchStatements);
        SqlAssert.ParseFails("begin try print 1 end try", SqlOptions.SqlServer);
        SqlAssert.ParseFails("begin try print 1 end catch", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_throw_with_arguments()
    {
        const string sql = "throw 50000, N'Not found', 1";
        var statement = SqlAssert.ParseTsql<TsqlThrowStatement>(sql);
        Assert.Equal(["50000", "N'Not found'", "1"], statement.Arguments.Select(argument => Text(sql, argument.Span)));
        Assert.Equal(sql, Text(sql, statement.Span));
        var batch = SqlAssert.ParseTsql<TsqlBatch>(ThrowAfterTerminator);
        Assert.Equal(Three, Assert.IsType<TsqlThrowStatement>(batch.Statements[1]).Arguments.Count);
        SqlAssert.ParseFails("throw 50000, 'x'", SqlOptions.SqlServer);
        FailsInIso("throw 50000, 'x', 1");
    }

    [Fact]
    public void Parses_raiserror_with_arguments_and_options()
    {
        const string sql = "raiserror (N'Row %d of %s', 16, 1, @row, @name) with log, nowait, seterror";
        var statement = SqlAssert.ParseTsql<TsqlRaiserrorStatement>(sql);
        Assert.NotNull(statement.OpenParen);
        Assert.Equal(Five, statement.Arguments.Count);
        Assert.Equal(["log", "nowait", "seterror"], Texts(sql, statement.Options));
        Assert.Equal(sql, Text(sql, statement.Span));
        var plain = SqlAssert.ParseTsql<TsqlRaiserrorStatement>("raiserror ('x', 10, 1)");
        Assert.Null(plain.WithKeyword);
        Assert.Equal(Three, plain.Arguments.Count);
    }

    [Fact]
    public void Parses_legacy_raiserror()
    {
        const string sql = "raiserror 50001 'legacy message'";
        var statement = SqlAssert.ParseTsql<TsqlRaiserrorStatement>(sql);
        Assert.Null(statement.OpenParen);
        Assert.Equal(["50001", "'legacy message'"], statement.Arguments.Select(argument => Text(sql, argument.Span)));
        Assert.Equal(sql, Text(sql, statement.Span));
        FailsInIso("raiserror ('x', 16, 1)");
    }

    [Fact]
    public void Parses_print()
    {
        const string sql = "print N'Count: ' + cast(@n as varchar(10))";
        var statement = SqlAssert.ParseTsql<TsqlPrintStatement>(sql);
        Assert.IsType<BinaryExpression>(statement.Value);
        Assert.Equal(sql, Text(sql, statement.Span));
        SqlAssert.ParseFails("print", SqlOptions.SqlServer);
        FailsInIso("print 'x'");
    }
}
