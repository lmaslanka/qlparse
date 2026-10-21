namespace QlParse.Tests;

// SQL Server administration commands (stream G): DBCC, BACKUP / RESTORE, CHECKPOINT, KILL, RECONFIGURE,
// SHUTDOWN, SETUSER and READTEXT / WRITETEXT / UPDATETEXT. Server-level DDL is in TsqlAdminTests.Server.cs,
// Service Broker in TsqlAdminTests.Broker.cs and full-text DDL in TsqlAdminTests.Fulltext.cs.
public sealed partial class TsqlAdminTests
{
    private const int Two = 2;
    private const int Three = 3;
    private const int Four = 4;
    private const string Disk = "disk";
    private const string Kill52 = "kill 52";
    private const string CheckpointWord = "checkpoint";

    private static T Whole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParseTsql<T>(sql);
        Assert.Equal(new SourceSpan(0, sql.Length), node.Span);
        return node;
    }

    private static Query AnyTsql(string sql)
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        Assert.Null(result.Error);
        return Assert.IsAssignableFrom<Query>(result.Root);
    }

    private static string Text(string sql, SyntaxToken? token) =>
        token is SyntaxToken value ? value.TextOf(sql).ToString() : string.Empty;

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Words(string sql, IReadOnlyList<SyntaxToken> tokens) =>
        string.Join(' ', tokens.Select(token => token.TextOf(sql).ToString()));

    private static string Name(string sql, TsqlOption option) => Words(sql, option.Name);

    private static string Value(string sql, TsqlOption option) => Words(sql, option.Value);

    // ---- DBCC ---------------------------------------------------------------------------------------

    [Fact]
    public void Parses_dbcc_with_arguments_and_options()
    {
        const string sql = "dbcc checkdb (N'db', repair_allow_data_loss) with no_infomsgs, all_errormsgs, maxdop = 4";
        var dbcc = Whole<TsqlDbccStatement>(sql);
        Assert.Equal("checkdb", Text(sql, dbcc.Command));
        Assert.Equal(Two, dbcc.Arguments.Count);
        Assert.IsType<IdentifierExpression>(dbcc.Arguments[1]);
        Assert.NotNull(dbcc.WithKeyword);
        var options = dbcc.Options!.Options;
        Assert.Equal(Three, options.Count);
        Assert.Equal("no_infomsgs", Name(sql, options[0]));
        Assert.Empty(options[0].Value);
        Assert.Equal("maxdop", Name(sql, options[Two]));
        Assert.NotNull(options[Two].EqualsToken);
        Assert.Equal("4", Value(sql, options[Two]));
    }

    [Theory]
    [InlineData("dbcc freeproccache", 0)]
    [InlineData("dbcc inputbuffer (52)", 1)]
    [InlineData("dbcc freeproccache (0x060006001ECA270EC0215D05000000000000000000000000)", 1)]
    [InlineData("dbcc shrinkfile (N'log', 1)", 2)]
    [InlineData("dbcc traceon (3604, -1)", 2)]
    [InlineData("dbcc checkident ('dbo.t', reseed, 0)", 3)]
    [InlineData("dbcc sqlperf(logspace)", 1)]
    [InlineData("dbcc show_statistics ('dbo.t', ix_t) with histogram", 2)]
    [InlineData("dbcc shrinkfile (f, emptyfile) with wait_at_low_priority (abort_after_wait = self)", 2)]
    public void Parses_dbcc_commands(string sql, int arguments)
    {
        var dbcc = Whole<TsqlDbccStatement>(sql);
        Assert.Equal(arguments, dbcc.Arguments.Count);
        Assert.Equal(arguments > 0, dbcc.OpenParen is not null);
    }

    [Fact]
    public void Dbcc_nested_option_list()
    {
        const string sql = "dbcc shrinkfile (f, 10) with wait_at_low_priority (max_duration = 1 minutes, abort_after_wait = self)";
        var option = Whole<TsqlDbccStatement>(sql).Options!.Options[0];
        Assert.Equal("wait_at_low_priority", Name(sql, option));
        var nested = option.Options!.Options;
        Assert.Equal(Two, nested.Count);
        Assert.Equal("1 minutes", Value(sql, nested[0]));
        Assert.Equal("self", Value(sql, nested[1]));
    }

    [Fact]
    public void Dbcc_needs_sql_server()
    {
        SqlAssert.ParseFails("dbcc checkdb");
        SqlAssert.ParseFails("dbcc checkdb", SqlOptions.Postgres);
    }

    // ---- BACKUP / RESTORE ---------------------------------------------------------------------------

    [Fact]
    public void Parses_backup_database_to_disk_with_options()
    {
        const string sql = "backup database AdventureWorks to disk = N'C:\\b\\aw.bak', disk = @second "
            + "mirror to url = 'https://x/aw.bak' with format, init, name = N'Full', compression, stats = 10, "
            + "encryption (algorithm = aes_256, server certificate = BackupCert)";
        var backup = Whole<TsqlBackupStatement>(sql);
        Assert.Equal("database", Text(sql, backup.TargetKind));
        Assert.Equal("AdventureWorks", Text(sql, backup.Database));
        Assert.Null(backup.Files);
        Assert.Equal(Two, backup.Devices.Count);
        Assert.Equal(Disk, Text(sql, backup.Devices[0].DeviceType));
        Assert.Equal("@second", Text(sql, backup.Devices[1].Name));
        var mirror = Assert.Single(backup.Mirrors);
        Assert.Equal("url", Text(sql, mirror.Devices[0].DeviceType));
        var options = backup.Options!.Options;
        Assert.Equal("format", Name(sql, options[0]));
        Assert.Equal("N'Full'", Value(sql, options[Two]));
        var encryption = options[^1];
        Assert.Equal("encryption", Name(sql, encryption));
        Assert.Equal("server certificate", Name(sql, encryption.Options!.Options[1]));
        Assert.Equal("BackupCert", Value(sql, encryption.Options.Options[1]));
    }

    [Fact]
    public void Parses_backup_log_and_files_and_logical_devices()
    {
        const string sql = "backup log @db to LogDevice with norecovery, no_truncate";
        var backup = Whole<TsqlBackupStatement>(sql);
        Assert.Equal("log", Text(sql, backup.TargetKind));
        Assert.Equal("@db", Text(sql, backup.Database));
        Assert.Null(backup.Devices[0].DeviceType);
        Assert.Equal("LogDevice", Text(sql, backup.Devices[0].Name));

        const string files = "backup database db file = 'f1', filegroup = 'fg' to disk = 'x.bak'";
        var fileBackup = Whole<TsqlBackupStatement>(files);
        Assert.Equal(Two, fileBackup.Files!.Options.Count);
        Assert.Equal("filegroup", Name(files, fileBackup.Files.Options[1]));
        Assert.Null(fileBackup.WithKeyword);
    }

    [Fact]
    public void Backup_of_keys_and_certificates_is_not_an_admin_backup()
    {
        var result = Sql.Parse("backup certificate c to file = 'c.cer'", SqlOptions.SqlServer);
        Assert.IsNotType<TsqlBackupStatement>(result.Root);
        SqlAssert.ParseFails("backup database db to disk = 'x.bak'");
    }

    [Fact]
    public void Parses_restore_database_with_move()
    {
        const string sql = "restore database db from disk = 'C:\\db.bak' with file = 2, "
            + "move 'db_data' to 'D:\\db.mdf', move 'db_log' to 'D:\\db.ldf', norecovery, replace, stats = 5";
        var restore = Whole<TsqlRestoreStatement>(sql);
        Assert.Equal("db", Text(sql, restore.Database));
        Assert.NotNull(restore.FromKeyword);
        Assert.Single(restore.Devices);
        var options = restore.Options!.Options;
        Assert.Equal("move", Name(sql, options[1]));
        Assert.Equal("'db_data' to 'D:\\db.mdf'", Value(sql, options[1]));
        Assert.Null(options[1].EqualsToken);
        Assert.Equal("norecovery", Name(sql, options[Three]));
    }

    [Theory]
    [InlineData("restore headeronly from disk = 'x.bak'", "headeronly")]
    [InlineData("restore filelistonly from disk = 'x.bak' with file = 1", "filelistonly")]
    [InlineData("restore verifyonly from url = 'https://x/y.bak' with credential = 'c', checksum", "verifyonly")]
    [InlineData("restore labelonly from tape = '\\\\.\\tape0'", "labelonly")]
    public void Parses_restore_only_forms(string sql, string kind)
    {
        var restore = Whole<TsqlRestoreStatement>(sql);
        Assert.Equal(kind, Text(sql, restore.TargetKind));
        Assert.Null(restore.Database);
        Assert.Single(restore.Devices);
    }

    [Fact]
    public void Parses_restore_without_from_and_with_pages_and_stopat()
    {
        const string recovery = "restore database db with recovery";
        var restore = Whole<TsqlRestoreStatement>(recovery);
        Assert.Null(restore.FromKeyword);
        Assert.Empty(restore.Devices);

        const string page = "restore database db page = '1:57, 1:202' from DbBackup with norecovery";
        Assert.Equal("page", Name(page, Whole<TsqlRestoreStatement>(page).Files!.Options[0]));

        const string stop = "restore log db from disk = 'l.trn' with stopatmark = 'lsn:15000000040000037' after '2024-01-01', recovery";
        var stopAt = Whole<TsqlRestoreStatement>(stop).Options!.Options[0];
        Assert.Equal(Three, stopAt.Value.Count);

        const string snapshot = "restore database db from database_snapshot = 'db_snap'";
        Assert.Equal("database_snapshot", Text(snapshot, Whole<TsqlRestoreStatement>(snapshot).Devices[0].DeviceType));
    }

    // ---- CHECKPOINT, KILL, RECONFIGURE, SHUTDOWN, SETUSER -------------------------------------------

    [Fact]
    public void Parses_checkpoint_with_and_without_duration()
    {
        Assert.Null(Whole<TsqlCheckpointStatement>(CheckpointWord).Duration);
        const string sql = "checkpoint 10";
        Assert.Equal("10", Text(sql, Whole<TsqlCheckpointStatement>(sql).Duration));
        Assert.IsType<CheckpointStatement>(SqlAssert.ParsePg<CheckpointStatement>(CheckpointWord));
    }

    [Fact]
    public void Parses_kill_forms()
    {
        var kill = Whole<TsqlKillStatement>(Kill52);
        Assert.Empty(kill.Qualifier);
        Assert.Equal("52", Text(Kill52, kill.Target));

        const string status = "kill 'D5499C66-E398-45CA-BF7E-DC9C194B48CF' with statusonly";
        var uow = Whole<TsqlKillStatement>(status);
        Assert.NotNull(uow.StatusOnlyKeyword);

        const string subscription = "kill query notification subscription all";
        Assert.Equal(Three, Whole<TsqlKillStatement>(subscription).Qualifier.Count);
        Assert.Equal(Two, Whole<TsqlKillStatement>("kill stats job 5").Qualifier.Count);
        SqlAssert.ParseFails(Kill52);
    }

    [Fact]
    public void Parses_reconfigure_shutdown_and_setuser()
    {
        Assert.Null(Whole<TsqlReconfigureStatement>("reconfigure").OverrideKeyword);
        Assert.NotNull(Whole<TsqlReconfigureStatement>("reconfigure with override").OverrideKeyword);
        Assert.Null(Whole<TsqlShutdownStatement>("shutdown").NowaitKeyword);
        Assert.NotNull(Whole<TsqlShutdownStatement>("shutdown with nowait").WithKeyword);
        Assert.Null(Whole<TsqlSetUserStatement>("setuser").User);
        const string sql = "setuser 'mary' with noreset";
        var setUser = Whole<TsqlSetUserStatement>(sql);
        Assert.Equal("'mary'", Text(sql, setUser.User));
        Assert.NotNull(setUser.NoResetKeyword);
        SqlAssert.ParseFails("shutdown with nowait");
    }

    // ---- Legacy text/image statements ---------------------------------------------------------------

    [Fact]
    public void Parses_readtext_writetext_and_updatetext()
    {
        const string read = "readtext pub_info.pr_info @ptr 1 25 holdlock";
        var readText = Whole<TsqlReadTextStatement>(read);
        Assert.Equal(Two, readText.Column.Count);
        Assert.IsType<EmbeddedHostExpression>(readText.TextPointer);
        Assert.NotNull(readText.HoldLockKeyword);

        const string write = "writetext bulk pub_info.pr_info @ptr with log 'new text'";
        var writeText = Whole<TsqlWriteTextStatement>(write);
        Assert.NotNull(writeText.BulkKeyword);
        Assert.NotNull(writeText.LogKeyword);
        Assert.IsType<LiteralExpression>(writeText.Data);

        const string update = "updatetext t.c @ptr null 0 with log t2.c2 @ptr2";
        var updateText = Whole<TsqlUpdateTextStatement>(update);
        Assert.IsType<LiteralExpression>(updateText.InsertOffset);
        Assert.NotNull(updateText.Data);
        Assert.IsType<EmbeddedHostExpression>(updateText.SourceTextPointer);

        const string delete = "updatetext t.c @ptr 0 null";
        Assert.Null(Whole<TsqlUpdateTextStatement>(delete).Data);
    }

    // ---- Statement boundaries and the visitor -------------------------------------------------------

    [Fact]
    public void Admin_statements_end_without_semicolons()
    {
        const string sql = "backup database db to disk = 'x.bak' with init\ncheckpoint\ndbcc checkdb\nkill 52\nselect 1";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(Kill52, Text(sql, batch.Statements[Three].Span));
        Assert.IsType<TsqlBackupStatement>(batch.Statements[0]);
        Assert.IsType<TsqlCheckpointStatement>(batch.Statements[1]);
        Assert.IsType<TsqlDbccStatement>(batch.Statements[Two]);
        Assert.IsType<SelectStatement>(batch.Statements[^1]);
    }

    private sealed class OptionCounter : SqlVisitor
    {
        public int Options { get; private set; }

        public int Expressions { get; private set; }

        public override void VisitTsqlOption(TsqlOption node)
        {
            Options++;
            base.VisitTsqlOption(node);
        }

        public override void VisitIdentifierExpression(IdentifierExpression node)
        {
            Expressions++;
            base.VisitIdentifierExpression(node);
        }
    }

    [Fact]
    public void Visitor_walks_admin_statements()
    {
        var counter = new OptionCounter();
        counter.Visit(AnyTsql(
            "dbcc checkident (t, reseed) with wait_at_low_priority (max_duration = 1, abort_after_wait = self)"));
        Assert.Equal(Three, counter.Options);
        Assert.Equal(Two, counter.Expressions);
        foreach (var sql in VisitorSamples)
        {
            new SqlVisitor().Visit(AnyTsql(sql));
        }
    }

    private static readonly string[] VisitorSamples =
    [
        "backup database db to disk = 'x' with stats = 1",
        "restore database db from disk = 'x' with recovery",
        CheckpointWord,
        Kill52,
        "reconfigure",
        "shutdown",
        "setuser",
        "readtext t.c @p 0 1",
        "writetext t.c @p 'x'",
        "updatetext t.c @p 0 null 'x'",
        "updatetext t.c @p 0 null t.d @q",
        "alter server configuration set softnuma off",
        "create availability group ag for database db replica on 'a' with (failover_mode = manual), "
            + "'b' with (failover_mode = manual) listener 'l' (with ip (('10.0.0.1', '255.0.0.0')), port = 1433)",
        "alter availability group ag add replica on 'c' with (availability_mode = asynchronous_commit)",
        "create endpoint e state = started as tcp (listener_port = 5022) for database_mirroring (role = all)",
        "create resource pool p with (max_cpu_percent = 50)",
        "create workload group g with (importance = high) using p",
        "alter resource governor with (classifier_function = dbo.fn)",
        "create event session s on server add event sqlserver.rpc_completed (set collect_statement = 1 "
            + "action (sqlserver.sql_text) where duration > 1000) add target package0.ring_buffer with (startup_state = on)",
    ];
}
