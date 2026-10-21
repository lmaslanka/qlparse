namespace QlParse.Tests;

// SQL Server server-level administration (stream G): ALTER SERVER CONFIGURATION, availability groups,
// endpoints, Resource Governor and extended events sessions.
public sealed partial class TsqlAdminTests
{
    private const string EndpointSql = "create endpoint e state = started as tcp (listener_port = 5022) "
        + "for database_mirroring (authentication = windows negotiate, encryption = required algorithm aes, role = all)";

    // ---- ALTER SERVER CONFIGURATION -----------------------------------------------------------------

    [Fact]
    public void Parses_alter_server_configuration_settings()
    {
        const string affinity = "alter server configuration set process affinity cpu = 0 to 3, 8 to 11";
        var config = Whole<TsqlAlterServerConfigurationStatement>(affinity);
        Assert.Equal("process affinity", Words(affinity, config.Setting));
        Assert.Equal(Two, config.Options.Options.Count);
        Assert.Equal("cpu", Name(affinity, config.Options.Options[0]));
        Assert.Equal("0 to 3", Value(affinity, config.Options.Options[0]));
        Assert.Empty(config.Options.Options[1].Name);
        Assert.Equal("8 to 11", Value(affinity, config.Options.Options[1]));

        const string buffer = "alter server configuration set buffer pool extension on (filename = 'F:\\x.bpe', size = 50 gb)";
        var bufferPool = Whole<TsqlAlterServerConfigurationStatement>(buffer);
        Assert.Equal("buffer pool extension", Words(buffer, bufferPool.Setting));
        var on = Assert.Single(bufferPool.Options.Options);
        Assert.Equal("on", Value(buffer, on));
        Assert.Equal("50 gb", Value(buffer, on.Options!.Options[1]));

        const string hadr = "alter server configuration set hadr cluster context = local";
        var context = Assert.Single(Whole<TsqlAlterServerConfigurationStatement>(hadr).Options.Options);
        Assert.Empty(context.Name);
        Assert.NotNull(context.EqualsToken);
        Assert.Equal("local", Value(hadr, context));
    }

    [Theory]
    [InlineData("alter server configuration set softnuma off", "softnuma")]
    [InlineData("alter server configuration set diagnostics log max_size = 10 mb", "diagnostics log")]
    [InlineData("alter server configuration set memory_optimized tempdb_metadata = on (resource_pool = 'p')", "memory_optimized")]
    [InlineData("alter server configuration set failover cluster property VerboseLogging = 2", "failover cluster property")]
    [InlineData("alter server configuration set suspend_for_snapshot_backup = on (group = (db1, db2), mode = copy_only)", "suspend_for_snapshot_backup")]
    public void Parses_server_configuration_setting_words(string sql, string setting)
    {
        Assert.Equal(setting, Words(sql, Whole<TsqlAlterServerConfigurationStatement>(sql).Setting));
    }

    [Fact]
    public void Alter_server_role_is_not_server_configuration()
    {
        var result = Sql.Parse("alter server role r add member m", SqlOptions.SqlServer);
        Assert.IsNotType<TsqlAlterServerConfigurationStatement>(result.Root);
        SqlAssert.ParseFails("alter server configuration set softnuma off");
    }

    // ---- Availability groups ------------------------------------------------------------------------

    [Fact]
    public void Parses_create_availability_group()
    {
        const string sql = "create availability group ag with (automated_backup_preference = secondary) "
            + "for database db1, db2 replica on "
            + "'srv1' with (endpoint_url = 'TCP://srv1:5022', availability_mode = synchronous_commit, "
            + "secondary_role (allow_connections = all, read_only_routing_url = 'TCP://srv1:1433')), "
            + "'srv2' with (endpoint_url = 'TCP://srv2:5022', primary_role (read_only_routing_list = ('srv1', 'srv2'))), "
            + "listener 'agl' (with ip (('10.0.0.10', '255.255.255.0')), port = 1433)";
        var group = Whole<TsqlCreateAvailabilityGroupStatement>(sql);
        Assert.Equal("ag", Text(sql, group.Name));
        Assert.Single(group.Options!.Options);
        Assert.Equal(Two, group.Databases.Count);
        Assert.Equal("replica on", Words(sql, group.ReplicaKeywords));
        Assert.Equal(Two, group.Replicas.Count);
        var secondary = group.Replicas[0].Options!.Options[Two];
        Assert.Equal("secondary_role", Name(sql, secondary));
        Assert.Equal("all", Value(sql, secondary.Options!.Options[0]));
        var routing = group.Replicas[1].Options!.Options[1].Options!.Options[0];
        Assert.Equal(Two, routing.Options!.Options.Count);
        Assert.Equal("'agl'", Text(sql, group.Listener!.Name));
        var ip = group.Listener.Options!.Options[0];
        Assert.Equal("with ip", Name(sql, ip));
        Assert.Equal(Two, Assert.Single(ip.Options!.Options).Options!.Options.Count);
    }

    [Fact]
    public void Parses_distributed_availability_group()
    {
        const string sql = "create availability group dag with (distributed) availability group on "
            + "'ag1' with (listener_url = 'tcp://ag1:5022', availability_mode = asynchronous_commit), "
            + "'ag2' with (listener_url = 'tcp://ag2:5022', availability_mode = asynchronous_commit)";
        var group = Whole<TsqlCreateAvailabilityGroupStatement>(sql);
        Assert.Equal("availability group on", Words(sql, group.ReplicaKeywords));
        Assert.Null(group.DatabaseKeyword);
        Assert.Equal(Two, group.Replicas.Count);
    }

    [Theory]
    [InlineData("alter availability group ag set (healthcheck_timeout = 60000)", "set", 0)]
    [InlineData("alter availability group ag add database db", "add database", 1)]
    [InlineData("alter availability group ag remove replica on 'srv3'", "remove replica on", 1)]
    [InlineData("alter availability group ag modify replica on 'srv2' with (secondary_role (allow_connections = read_only))", "modify replica on", 1)]
    [InlineData("alter availability group ag join", "join", 0)]
    [InlineData("alter availability group ag failover", "failover", 0)]
    [InlineData("alter availability group ag force_failover_allow_data_loss", "force_failover_allow_data_loss", 0)]
    [InlineData("alter availability group ag grant create any database", "grant create any database", 0)]
    [InlineData("alter availability group ag add listener 'l' (with dhcp on ('10.0.0.0', '255.0.0.0'))", "add listener", 1)]
    [InlineData("alter availability group ag restart listener 'l'", "restart listener", 1)]
    [InlineData("alter availability group dag join availability group on 'ag1' with (listener_url = 'x'), 'ag2' with (listener_url = 'y')", "join availability group on", 2)]
    public void Parses_alter_availability_group_actions(string sql, string action, int targets)
    {
        var alter = Whole<TsqlAlterAvailabilityGroupStatement>(sql);
        Assert.Equal(action, Words(sql, alter.Action));
        Assert.Equal(targets, alter.Targets.Count);
    }

    // ---- Endpoints ----------------------------------------------------------------------------------

    [Fact]
    public void Parses_create_endpoint()
    {
        var endpoint = Whole<TsqlEndpointStatement>(EndpointSql);
        Assert.Equal("create", Text(EndpointSql, endpoint.CreateOrAlterKeyword));
        Assert.Equal("state", Name(EndpointSql, endpoint.State!));
        Assert.Equal("started", Value(EndpointSql, endpoint.State!));
        Assert.Equal("tcp", Text(EndpointSql, endpoint.Protocol));
        Assert.Single(endpoint.ProtocolOptions!.Options);
        Assert.Equal("database_mirroring", Text(EndpointSql, endpoint.Payload));
        var payload = endpoint.PayloadOptions!.Options;
        Assert.Equal(Three, payload.Count);
        Assert.Equal("windows negotiate", Value(EndpointSql, payload[0]));
        Assert.Equal("required algorithm aes", Value(EndpointSql, payload[1]));
    }

    [Fact]
    public void Parses_alter_endpoint_and_empty_payload()
    {
        const string sql = "alter endpoint e authorization sa state = stopped";
        var endpoint = Whole<TsqlEndpointStatement>(sql);
        Assert.Equal("sa", Text(sql, endpoint.Owner));
        Assert.Null(endpoint.AsKeyword);
        const string tsql = "create endpoint e as tcp (listener_port = 1433, listener_ip = all) for tsql ()";
        Assert.Empty(Whole<TsqlEndpointStatement>(tsql).PayloadOptions!.Options);
        SqlAssert.ParseFails(EndpointSql);
    }

    // ---- Resource Governor --------------------------------------------------------------------------

    [Fact]
    public void Parses_resource_pools()
    {
        const string sql = "create resource pool p with (min_cpu_percent = 10, max_cpu_percent = 20, "
            + "affinity scheduler = (0 to 3, 5), max_memory_percent = 30)";
        var pool = Whole<TsqlResourcePoolStatement>(sql);
        Assert.Null(pool.ExternalKeyword);
        var affinity = pool.Options!.Options[Two];
        Assert.Equal("affinity scheduler", Name(sql, affinity));
        Assert.Equal(Two, affinity.Options!.Options.Count);
        Assert.Equal("0 to 3", Value(sql, affinity.Options.Options[0]));

        const string external = "alter external resource pool ep with (max_cpu_percent = 50, affinity cpu = auto)";
        Assert.NotNull(Whole<TsqlResourcePoolStatement>(external).ExternalKeyword);
        Assert.Null(Whole<TsqlResourcePoolStatement>("create resource pool p").Options);
    }

    [Fact]
    public void Parses_workload_groups()
    {
        const string sql = "create workload group g with (importance = medium, request_max_memory_grant_percent = 25) "
            + "using p, external ep";
        var group = Whole<TsqlWorkloadGroupStatement>(sql);
        Assert.Equal(Two, group.Options!.Options.Count);
        Assert.Equal("p", Text(sql, group.Pool));
        Assert.Equal("ep", Text(sql, group.ExternalPool));
        const string quoted = "alter workload group g using \"default\"";
        Assert.Equal("\"default\"", Text(quoted, Whole<TsqlWorkloadGroupStatement>(quoted).Pool));
    }

    [Theory]
    [InlineData("alter resource governor reconfigure", "reconfigure")]
    [InlineData("alter resource governor disable", "disable")]
    [InlineData("alter resource governor reset statistics", "reset statistics")]
    [InlineData("alter resource governor with (classifier_function = dbo.classifier)", "")]
    [InlineData("alter resource governor with (classifier_function = null)", "")]
    public void Parses_alter_resource_governor(string sql, string action)
    {
        var governor = Whole<TsqlAlterResourceGovernorStatement>(sql);
        Assert.Equal(action, Words(sql, governor.Action));
        Assert.Equal(action.Length == 0, governor.Options is not null);
    }

    // ---- Extended events ----------------------------------------------------------------------------

    [Fact]
    public void Parses_create_event_session()
    {
        const string sql = "create event session [slow] on server "
            + "add event sqlserver.sql_statement_completed (set collect_statement = (1) "
            + "action (sqlserver.sql_text, sqlserver.client_app_name) "
            + "where duration > 1000 and sqlserver.like_i_sql_unicode_string(sqlserver.sql_text, N'%x%')), "
            + "add event sqlserver.error_reported "
            + "add target package0.event_file (set filename = N'slow.xel', max_file_size = (5)) "
            + "with (max_memory = 4096 kb, event_retention_mode = allow_single_event_loss, startup_state = on)";
        var session = Whole<TsqlEventSessionStatement>(sql);
        Assert.Equal("server", Text(sql, session.Scope));
        Assert.Equal(Three, session.Items.Count);
        var completed = session.Items[0];
        Assert.Equal("sqlserver sql_statement_completed", Words(sql, completed.Name));
        Assert.Single(completed.Settings!.Options);
        Assert.Equal(Two, completed.Actions.Count);
        Assert.IsType<BinaryExpression>(completed.Predicate);
        Assert.Null(session.Items[1].OpenParen);
        var target = session.Items[Two];
        Assert.Equal("target", Text(sql, target.Kind));
        Assert.Equal(Two, target.Settings!.Options.Count);
        Assert.Equal("4096 kb", Value(sql, session.Options!.Options[0]));
    }

    [Fact]
    public void Parses_alter_event_session()
    {
        const string state = "alter event session s on server state = start";
        var started = Whole<TsqlEventSessionStatement>(state);
        Assert.Empty(started.Items);
        Assert.Equal("start", Value(state, started.State!));

        const string sql = "alter event session s on database drop event sqlserver.error_reported, "
            + "add target package0.ring_buffer";
        var altered = Whole<TsqlEventSessionStatement>(sql);
        Assert.Equal("drop", Text(sql, altered.Items[0].Action));
        Assert.Equal(Two, altered.Items.Count);
    }

    [Fact]
    public void Event_notification_is_not_an_event_session()
    {
        var result = Sql.Parse("create event notification n on server for create_database to service 's', 'current database'", SqlOptions.SqlServer);
        Assert.IsNotType<TsqlEventSessionStatement>(result.Root);
    }
}
