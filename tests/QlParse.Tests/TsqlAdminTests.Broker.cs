namespace QlParse.Tests;

// SQL Server Service Broker (stream G): message types, contracts, queues, services, routes and the
// conversation statements.
public sealed partial class TsqlAdminTests
{
    private const string AlterVerb = "alter";
    private const string HandleVariable = "@h";
    private const string GroupVariable = "@g";
    private const string SendSql = "send on conversation @h message type [//x/Request] (@body)";
    private const string ReceiveSql = "receive top (1) @h = conversation_handle, message_type_name as type, "
        + "cast(message_body as xml) body from dbo.TargetQueue into @t where conversation_group_id = @g";

    // ---- Objects ------------------------------------------------------------------------------------

    [Fact]
    public void Parses_message_types()
    {
        const string sql = "create message type [//Adventure-Works.com/Expenses/Submit] authorization dbo "
            + "validation = valid_xml with schema collection dbo.ExpenseSchema";
        var type = Whole<TsqlMessageTypeStatement>(sql);
        Assert.Equal("[//Adventure-Works.com/Expenses/Submit]", Text(sql, type.Name));
        Assert.Equal("dbo", Text(sql, type.Owner));
        Assert.Equal("valid_xml", Text(sql, type.Validation));
        Assert.Equal(Two, type.SchemaCollection.Count);

        const string alter = "alter message type m validation = well_formed_xml";
        var altered = Whole<TsqlMessageTypeStatement>(alter);
        Assert.Equal(AlterVerb, Text(alter, altered.CreateOrAlterKeyword));
        Assert.Empty(altered.SchemaCollection);
        Assert.Null(Whole<TsqlMessageTypeStatement>("create message type m").ValidationKeyword);
    }

    [Fact]
    public void Parses_contracts()
    {
        const string sql = "create contract [//x/Contract] authorization dbo "
            + "([//x/Request] sent by initiator, [//x/Reply] sent by target, [DEFAULT] sent by any)";
        var contract = Whole<TsqlCreateContractStatement>(sql);
        Assert.Equal(Three, contract.Messages.Count);
        Assert.Equal("initiator", Text(sql, contract.Messages[0].Sender));
        Assert.Equal("any", Text(sql, contract.Messages[Two].Sender));
        SqlAssert.ParseFails("create contract c ([m] sent by)", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_create_queue()
    {
        const string sql = "create queue db.dbo.ExpenseQueue with status = on, retention = off, "
            + "activation (status = on, procedure_name = dbo.expense_procedure, max_queue_readers = 5, execute as self), "
            + "poison_message_handling (status = off) on [DEFAULT]";
        var queue = Whole<TsqlCreateQueueStatement>(sql);
        Assert.Equal(Three, queue.Name.Count);
        var options = queue.Options!.Options;
        Assert.Equal(Four, options.Count);
        Assert.Equal("on", Value(sql, options[0]));
        var activation = options[Two].Options!.Options;
        Assert.Equal("dbo . expense_procedure", Value(sql, activation[1]));
        Assert.Equal("execute as", Name(sql, activation[Three]));
        Assert.Equal("self", Value(sql, activation[Three]));
        Assert.Equal("[DEFAULT]", Text(sql, queue.FileGroup));
        Assert.Null(Whole<TsqlCreateQueueStatement>("create queue q").Options);
    }

    [Theory]
    [InlineData("alter queue q with status = off", "", 1)]
    [InlineData("alter queue q rebuild with (maxdop = 2)", "rebuild", 1)]
    [InlineData("alter queue q reorganize", "reorganize", 0)]
    [InlineData("alter queue q move to [DEFAULT]", "move to", 0)]
    public void Parses_alter_queue(string sql, string action, int options)
    {
        var queue = Whole<TsqlAlterQueueStatement>(sql);
        Assert.Equal(action, Words(sql, queue.Action));
        Assert.Equal(options, queue.Options?.Options.Count ?? 0);
    }

    [Fact]
    public void Parses_services()
    {
        const string sql = "create service [//x/Service] authorization dbo on queue dbo.q ([//x/Contract], [DEFAULT])";
        var service = Whole<TsqlServiceStatement>(sql);
        Assert.Equal(Two, service.Queue.Count);
        Assert.Equal(Two, service.Contracts.Count);
        Assert.Null(service.Contracts[0].Action);

        const string alter = "alter service s on queue q2 (add contract c1, drop contract c2)";
        var altered = Whole<TsqlServiceStatement>(alter);
        Assert.Equal("drop", Text(alter, altered.Contracts[1].Action));
        Assert.Empty(Whole<TsqlServiceStatement>("create service s on queue q").Contracts);
    }

    [Fact]
    public void Alter_service_master_key_is_not_a_service()
    {
        var result = Sql.Parse("alter service master key regenerate", SqlOptions.SqlServer);
        Assert.IsNotType<TsqlServiceStatement>(result.Root);
    }

    [Fact]
    public void Parses_routes()
    {
        const string sql = "create route r with service_name = '//x/Service', "
            + "broker_instance = 'D8D4D268-02A3-4C62-8F91-634B89C1E315', lifetime = 259200, address = 'TCP://srv:4321'";
        var route = Whole<TsqlRouteStatement>(sql);
        Assert.Equal(Four, route.Options.Options.Count);
        Assert.Equal("259200", Value(sql, route.Options.Options[Two]));
        Assert.Equal(AlterVerb, Text("alter route r with address = 'LOCAL'", Whole<TsqlRouteStatement>("alter route r with address = 'LOCAL'").CreateOrAlterKeyword));
        SqlAssert.ParseFails("create route r with address = 'LOCAL'");
    }

    // ---- Conversations ------------------------------------------------------------------------------

    [Fact]
    public void Parses_begin_dialog()
    {
        const string sql = "begin dialog conversation @h from service [//x/Initiator] to service '//x/Target', "
            + "'CURRENT DATABASE' on contract [//x/Contract] with related_conversation = @other, lifetime = 600, encryption = off";
        var dialog = Whole<TsqlBeginDialogStatement>(sql);
        Assert.Equal(HandleVariable, Text(sql, dialog.Handle));
        Assert.Equal("'//x/Target'", Text(sql, dialog.TargetService));
        Assert.Equal("'CURRENT DATABASE'", Text(sql, dialog.BrokerInstance));
        Assert.Equal("[//x/Contract]", Text(sql, dialog.Contract));
        Assert.Equal(Three, dialog.Options!.Options.Count);
        Assert.Equal("off", Value(sql, dialog.Options.Options[Two]));

        const string bare = "begin dialog @h from service s to service 't'";
        Assert.Null(Whole<TsqlBeginDialogStatement>(bare).ConversationKeyword);
    }

    [Fact]
    public void Parses_conversation_timer_send_and_move()
    {
        const string timer = "begin conversation timer (@h) timeout = 120";
        Assert.Equal("120", Text(timer, Whole<TsqlBeginConversationTimerStatement>(timer).Timeout));

        var send = Whole<TsqlSendStatement>(SendSql);
        Assert.Single(send.Handles);
        Assert.Equal("[//x/Request]", Text(SendSql, send.MessageType));
        Assert.IsType<EmbeddedHostExpression>(send.Body);
        const string many = "send on conversation (@h1, @h2) (N'<x/>')";
        var multi = Whole<TsqlSendStatement>(many);
        Assert.Equal(Two, multi.Handles.Count);
        Assert.Null(multi.MessageType);

        const string move = "move conversation @h to @g";
        Assert.Equal(GroupVariable, Text(move, Whole<TsqlMoveConversationStatement>(move).Group));
    }

    [Fact]
    public void Parses_receive()
    {
        var receive = Whole<TsqlReceiveStatement>(ReceiveSql);
        Assert.IsType<LiteralExpression>(receive.Top);
        Assert.Equal(Three, receive.Columns.Count);
        Assert.Equal(HandleVariable, Text(ReceiveSql, receive.Columns[0].Target));
        Assert.Equal("type", Text(ReceiveSql, receive.Columns[1].Alias));
        Assert.NotNull(receive.Columns[1].AsKeyword);
        Assert.Equal("body", Text(ReceiveSql, receive.Columns[Two].Alias));
        Assert.Equal(Two, receive.Queue.Count);
        Assert.Equal("@t", Text(ReceiveSql, receive.Into));
        Assert.IsType<BinaryExpression>(receive.Where);

        var star = Whole<TsqlReceiveStatement>("receive * from q");
        Assert.IsType<StarExpression>(star.Columns[0].Expression);
        SqlAssert.ParseFails("receive * from q");
    }

    [Fact]
    public void Parses_end_conversation_and_get_conversation_group()
    {
        const string error = "end conversation @h with error = 31472 description = 'Order not found'";
        var ended = Whole<TsqlEndConversationStatement>(error);
        Assert.Equal("31472", Text(error, ended.ErrorCode));
        Assert.Equal("'Order not found'", Text(error, ended.Description));
        Assert.NotNull(Whole<TsqlEndConversationStatement>("end conversation @h with cleanup").CleanupKeyword);
        Assert.Null(Whole<TsqlEndConversationStatement>("end conversation @h").WithKeyword);

        const string get = "get conversation group @g from dbo.q";
        var group = Whole<TsqlGetConversationGroupStatement>(get);
        Assert.Equal(GroupVariable, Text(get, group.Group));
        Assert.Equal(Two, group.Queue.Count);
    }

    [Fact]
    public void Conversation_statements_split_without_semicolons()
    {
        const string sql = "receive top (1) @h = conversation_handle from q\nend conversation @h\nmove conversation @a to @b";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.IsType<TsqlReceiveStatement>(batch.Statements[0]);
        Assert.IsType<TsqlEndConversationStatement>(batch.Statements[1]);
        Assert.IsType<TsqlMoveConversationStatement>(batch.Statements[Two]);
    }

    [Fact]
    public void Visitor_walks_broker_statements()
    {
        foreach (var sql in BrokerSamples)
        {
            new SqlVisitor().Visit(AnyTsql(sql));
        }

        var counter = new OptionCounter();
        counter.Visit(AnyTsql(ReceiveSql));
        Assert.Equal(Four, counter.Expressions);
    }

    private static readonly string[] BrokerSamples =
    [
        "create message type m",
        "create contract c (m sent by any)",
        "create queue q with activation (status = on)",
        "alter queue q rebuild with (maxdop = 1)",
        "create service s on queue q (c)",
        "create route r with address = 'LOCAL'",
        "begin dialog @h from service s to service 't' with lifetime = 1",
        "begin conversation timer (@h) timeout = 1",
        SendSql,
        "end conversation @h",
        "get conversation group @g from q",
        "move conversation @h to @g",
    ];
}
