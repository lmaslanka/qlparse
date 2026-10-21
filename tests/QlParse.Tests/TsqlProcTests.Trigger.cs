namespace QlParse.Tests;

// SQL Server triggers (stream E): DML, DDL and logon triggers, inserted / deleted, UPDATE(col),
// COLUMNS_UPDATED(), EVENTDATA(), ENABLE / DISABLE TRIGGER and CREATE EVENT NOTIFICATION.
public sealed partial class TsqlProcTests
{
    [Fact]
    public void Parses_dml_trigger()
    {
        const string sql = "create trigger dbo.tr_audit on dbo.orders with encryption after insert, update, delete not for replication as "
            + "if update(status) or columns_updated() & 2 > 0 insert into audit select i.id, d.status from inserted i join deleted d on i.id = d.id";
        var statement = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(sql);
        Assert.Equal(["dbo", "tr_audit"], Texts(sql, statement.Name));
        Assert.Equal(["dbo", "orders"], Texts(sql, statement.Target));
        Assert.Equal(["encryption"], Texts(sql, Assert.Single(statement.Options).Name));
        Assert.Equal("after", Text(sql, statement.TimingKeyword));
        Assert.Equal(["insert", "update", "delete"], Texts(sql, statement.Events));
        Assert.NotNull(statement.NotKeyword);
        Assert.Equal("replication", Text(sql, Assert.NotNull(statement.ReplicationKeyword)));
        var check = Assert.IsType<TsqlIfStatement>(Assert.Single(statement.Statements));
        var condition = Assert.IsType<BinaryExpression>(check.Condition);
        var update = Assert.IsType<FunctionCallExpression>(condition.Left);
        Assert.Equal("update", Text(sql, update.Name));
        Assert.IsType<IdentifierExpression>(Assert.Single(update.Arguments));
        var insert = Assert.IsType<InsertStatement>(check.Then);
        var select = Assert.IsType<SelectStatement>(insert.Query);
        Assert.Equal(["inserted"], Texts(sql, Assert.IsType<TableReference>(select.From).NameParts));
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_instead_of_and_for_triggers()
    {
        const string sql = "create or alter trigger tr on v instead of insert as insert into t select * from inserted";
        var instead = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(sql);
        Assert.Equal("instead", Text(sql, instead.TimingKeyword));
        Assert.NotNull(instead.OfKeyword);
        Assert.NotNull(instead.OrKeyword);
        const string forSql = "alter trigger tr on t for update with append as rollback";
        var legacy = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(forSql);
        Assert.Equal("for", Text(forSql, legacy.TimingKeyword));
        Assert.Equal("append", Text(forSql, Assert.NotNull(legacy.AppendKeyword)));
        Assert.Null(legacy.CreateKeyword);
        Assert.IsType<TsqlTransactionStatement>(Assert.Single(legacy.Statements));
    }

    [Fact]
    public void Update_function_only_parses_under_the_flag()
    {
        var call = Assert.IsType<FunctionCallExpression>(SqlAssert.ExprTsql("update(a)"));
        Assert.Single(call.Arguments);
        SqlAssert.ParseFails("select update(a)");
        SqlAssert.ParseFails("select update", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_ddl_trigger_with_eventdata()
    {
        const string sql = "create trigger tr_ddl on database for create_table, alter_table, ddl_index_events as "
            + "select eventdata().value('(/EVENT_INSTANCE/EventType)[1]', 'nvarchar(100)')";
        var statement = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(sql);
        Assert.Equal(DatabaseWord, Text(sql, Assert.NotNull(statement.DatabaseKeyword)));
        Assert.Empty(statement.Target);
        Assert.Equal(["create_table", "alter_table", "ddl_index_events"], Texts(sql, statement.Events));
        Assert.IsType<SelectStatement>(Assert.Single(statement.Statements));
    }

    [Fact]
    public void Parses_logon_trigger()
    {
        const string sql = "create trigger tr_logon on all server with execute as 'sa' for logon as if original_login() = 'x' rollback";
        var statement = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(sql);
        Assert.NotNull(statement.AllKeyword);
        Assert.Equal("server", Text(sql, Assert.NotNull(statement.ServerKeyword)));
        Assert.Equal(["'sa'"], Texts(sql, Assert.Single(statement.Options).Value));
        Assert.Equal(["logon"], Texts(sql, statement.Events));
    }

    [Fact]
    public void Parses_clr_trigger()
    {
        const string sql = "create trigger tr on t after insert as external name asm.cls.OnInsert";
        var statement = SqlAssert.ParseTsql<TsqlCreateTriggerStatement>(sql);
        Assert.Equal(["asm", "cls", "OnInsert"], Texts(sql, Present(statement.ExternalName).Name));
        Assert.Empty(statement.Statements);
    }

    [Fact]
    public void Trigger_needs_timing_and_the_flag()
    {
        SqlAssert.ParseFails("create trigger tr on t insert as select 1", SqlOptions.SqlServer);
        SqlAssert.ParseFails("create trigger tr on t after as select 1", SqlOptions.SqlServer);
        FailsInIso("create trigger tr on t after insert as select 1");
    }

    [Fact]
    public void Parses_enable_and_disable_trigger()
    {
        const string sql = "disable trigger dbo.tr1, tr2 on dbo.t enable trigger all on database disable trigger tr_logon on all server";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var first = Assert.IsType<TsqlToggleTriggerStatement>(batch.Statements[0]);
        Assert.Equal("disable", Text(sql, first.ToggleKeyword));
        Assert.Equal(Two, first.Triggers.Count);
        Assert.Equal(["dbo", "t"], Texts(sql, first.Target));
        var second = Assert.IsType<TsqlToggleTriggerStatement>(batch.Statements[1]);
        Assert.NotNull(second.AllTriggersKeyword);
        Assert.NotNull(second.DatabaseKeyword);
        var third = Assert.IsType<TsqlToggleTriggerStatement>(batch.Statements[Two]);
        Assert.NotNull(third.ServerKeyword);
        FailsInIso("enable trigger tr on t");
    }

    [Fact]
    public void Parses_create_event_notification()
    {
        const string sql = "create event notification log_ddl on database with fan_in for create_table, ddl_login_events "
            + "to service 'NotifyService', 'current database'";
        var statement = SqlAssert.ParseTsql<TsqlCreateEventNotificationStatement>(sql);
        Assert.Equal("log_ddl", Text(sql, statement.Name));
        Assert.Equal(DatabaseWord, Text(sql, statement.Scope));
        Assert.NotNull(statement.FanInKeyword);
        Assert.Equal(Two, statement.Events.Count);
        Assert.Equal("'NotifyService'", Text(sql, statement.Service));
        Assert.Equal("'current database'", Text(sql, statement.BrokerInstance));
        Assert.Equal(sql, Text(sql, statement.Span));
        const string queueSql = "create event notification n on queue dbo.q for queue_activation to service 's', 'current database'";
        var queue = SqlAssert.ParseTsql<TsqlCreateEventNotificationStatement>(queueSql);
        Assert.Equal(["dbo", "q"], Texts(queueSql, queue.Queue));
        FailsInIso("create event notification n on server for logon to service 's', 'current database'");
    }
}
