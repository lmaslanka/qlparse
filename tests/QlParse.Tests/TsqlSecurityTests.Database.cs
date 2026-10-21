namespace QlParse.Tests;

// Workstream F: SQL Server databases, schemas, and other objects (XML schema collections, assemblies, defaults and
// rules, PolyBase and AI external objects).
public sealed partial class TsqlSecurityTests
{
    private const string CreateDatabaseFull =
        "create database d containment = partial on primary (name = d_dat, filename = 'c:\\d.mdf', size = 10MB, "
        + "maxsize = unlimited, filegrowth = 10%), (name = d2, filename = 'x'), filegroup fg1 contains filestream default "
        + "(name = f, filename = 'y') log on (name = d_log, filename = 'c:\\d.ldf', size = 5 MB) "
        + "collate Latin1_General_CI_AS with db_chaining on, trustworthy off, "
        + "filestream (non_transacted_access = full, directory_name = N'x')";

    // ---- CREATE DATABASE ----------------------------------------------------------------------

    [Fact]
    public void Parses_create_database_with_files_filegroups_and_options()
    {
        const string sql = CreateDatabaseFull;
        var create = Whole<TsqlCreateDatabaseStatement>(sql);
        Assert.Equal(DatabaseName, Text(sql, create.Name));
        Assert.Equal("partial", Value(sql, create.Containment!));
        Assert.Equal(TwoItems, create.Filegroups.Count);
        var primary = create.Filegroups[0];
        Assert.Equal(PrimaryWord, Text(sql, primary.PrimaryKeyword));
        Assert.Equal(TwoItems, primary.Files.Count);
        var size = primary.Files[0].Items[TwoItems];
        Assert.Equal("size", Words(sql, size));
        Assert.Equal("10 MB", Value(sql, size));
        Assert.Equal("10 %", Value(sql, primary.Files[0].Items[^1]));
        var filegroup = create.Filegroups[1];
        Assert.Equal(FilegroupName, Text(sql, filegroup.Name));
        Assert.Equal("filestream", Text(sql, filegroup.ContainsKind));
        Assert.NotNull(filegroup.DefaultKeyword);
        Assert.Single(create.LogFiles);
        Assert.Equal("Latin1_General_CI_AS", Text(sql, create.Collation));
        Assert.Equal(["db_chaining", "trustworthy", "filestream"], create.Options.Select(option => Words(sql, option)));
        Assert.Equal(OnWord, Value(sql, create.Options[0]));
        Assert.Equal(TwoItems, create.Options[^1].Options!.Items.Count);
    }

    [Fact]
    public void Parses_create_database_snapshot_and_attach()
    {
        const string sql = "create database snap on (name = d, filename = 'c:\\s.ss') as snapshot of d";
        var snapshot = Whole<TsqlCreateDatabaseStatement>(sql);
        Assert.Null(snapshot.Filegroups[0].PrimaryKeyword);
        Assert.Equal(DatabaseName, Text(sql, snapshot.Source));
        const string attachSql = "create database d on (filename = 'c:\\a.mdf') for attach with enable_broker";
        var attach = Whole<TsqlCreateDatabaseStatement>(attachSql);
        Assert.Equal("attach", Text(attachSql, attach.AttachKeyword));
        Assert.Equal("enable_broker", Words(attachSql, attach.Options[0]));
        const string rebuildSql = "create database d on (filename = 'c:\\a.mdf') for attach_rebuild_log";
        Assert.Equal("attach_rebuild_log", Text(rebuildSql, Whole<TsqlCreateDatabaseStatement>(rebuildSql).AttachKeyword));
        Assert.Empty(Whole<TsqlCreateDatabaseStatement>("create database d").Filegroups);
    }

    // ---- ALTER DATABASE -----------------------------------------------------------------------

    [Fact]
    public void Parses_alter_database_set_with_termination()
    {
        const string sql = "alter database d set recovery simple, auto_close off with rollback immediate";
        var alter = Whole<TsqlAlterDatabaseStatement>(sql);
        Assert.Equal("set", Words(sql, alter.ActionKeywords));
        Assert.Equal(["recovery", "auto_close"], alter.Options.Select(option => Words(sql, option)));
        Assert.Equal("simple", Value(sql, alter.Options[0]));
        Assert.Equal("rollback immediate", Words(sql, alter.Termination));
        const string afterSql = "alter database current set single_user with rollback after 10 seconds";
        var after = Whole<TsqlAlterDatabaseStatement>(afterSql);
        Assert.Equal("current", Text(afterSql, after.Name));
        Assert.Equal("rollback after 10 seconds", Words(afterSql, after.Termination));
        const string storeSql = "alter database d set query_store = on (operation_mode = read_write, max_storage_size_mb = 100)";
        var store = Whole<TsqlAlterDatabaseStatement>(storeSql).Options[0];
        Assert.Equal(OnWord, Value(storeSql, store));
        Assert.Equal(TwoItems, store.Options!.Items.Count);
        const string retentionSql = "alter database d set change_tracking = on (change_retention = 2 days, auto_cleanup = on)";
        Assert.Equal("2 days", Value(retentionSql, Whole<TsqlAlterDatabaseStatement>(retentionSql).Options[0].Options!.Items[0]));
    }

    [Fact]
    public void Parses_alter_database_file_actions()
    {
        const string sql = "alter database d add file (name = f2, filename = 'f2.ndf', size = 5MB), (name = f3, filename = 'x') to filegroup fg1";
        var add = Whole<TsqlAlterDatabaseStatement>(sql);
        Assert.Equal("add file", Words(sql, add.ActionKeywords));
        Assert.Equal(TwoItems, add.Files.Count);
        Assert.Equal(FilegroupName, Text(sql, add.Filegroup));
        const string logSql = "alter database d add log file (name = l2, filename = 'l2.ldf')";
        Assert.Equal("add log file", Words(logSql, Whole<TsqlAlterDatabaseStatement>(logSql).ActionKeywords));
        const string removeSql = "alter database d remove file f2";
        Assert.Equal("f2", Text(removeSql, Whole<TsqlAlterDatabaseStatement>(removeSql).Target));
        const string modifySql = "alter database d modify file (name = f2, size = 20MB)";
        Assert.Single(Whole<TsqlAlterDatabaseStatement>(modifySql).Files);
    }

    [Fact]
    public void Parses_alter_database_filegroups_name_and_collation()
    {
        const string sql = "alter database d add filegroup fg2 contains memory_optimized_data";
        var add = Whole<TsqlAlterDatabaseStatement>(sql);
        Assert.Equal("add filegroup", Words(sql, add.ActionKeywords));
        Assert.Equal("fg2", Text(sql, add.Target));
        Assert.Equal("memory_optimized_data", Value(sql, add.Options[0]));
        const string readOnlySql = "alter database d modify filegroup fg2 read_only";
        Assert.Equal("read_only", Words(readOnlySql, Whole<TsqlAlterDatabaseStatement>(readOnlySql).Options[0]));
        const string renameSql = "alter database d modify filegroup fg2 name = fg3";
        Assert.Equal("fg3", Value(renameSql, Whole<TsqlAlterDatabaseStatement>(renameSql).Options[0]));
        Assert.Empty(Whole<TsqlAlterDatabaseStatement>("alter database d remove filegroup fg2").Options);
        const string nameSql = "alter database d modify name = d2";
        var name = Whole<TsqlAlterDatabaseStatement>(nameSql);
        Assert.Equal("modify name", Words(nameSql, name.ActionKeywords));
        Assert.Equal("d2", Text(nameSql, name.Target));
        const string collateSql = "alter database d collate SQL_Latin1_General_CP1_CI_AS";
        Assert.Equal("collate", Words(collateSql, Whole<TsqlAlterDatabaseStatement>(collateSql).ActionKeywords));
    }

    [Fact]
    public void Parses_alter_database_scoped_configuration()
    {
        const string sql = "alter database scoped configuration for secondary set maxdop = primary";
        var alter = Whole<TsqlAlterDatabaseScopedConfigurationStatement>(sql);
        Assert.NotNull(alter.SecondaryKeyword);
        Assert.Equal("set", Text(sql, alter.ActionKeyword));
        Assert.Equal("maxdop", Words(sql, alter.Option));
        Assert.Equal(PrimaryWord, Value(sql, alter.Option));
        const string clearSql = "alter database scoped configuration clear procedure_cache";
        var clear = Whole<TsqlAlterDatabaseScopedConfigurationStatement>(clearSql);
        Assert.Equal("clear", Text(clearSql, clear.ActionKeyword));
        Assert.Equal("procedure_cache", Words(clearSql, clear.Option));
    }

    [Theory]
    [InlineData("create database d on primary")]
    [InlineData("create database d on (name = )")]
    [InlineData("alter database d set")]
    [InlineData("alter database d frobnicate")]
    [InlineData("alter database d set single_user with rollback")]
    [InlineData("alter database scoped configuration set")]
    public void Rejects_malformed_database_statements(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Theory]
    [InlineData("create database d on primary (name = d, filename = 'f')")]
    [InlineData("alter database d set recovery simple")]
    [InlineData("alter database scoped configuration set maxdop = 4")]
    [InlineData("alter schema s transfer dbo.t")]
    [InlineData("alter authorization on object::t to u")]
    [InlineData("create schema s create view v as select 1")]
    public void Database_syntax_is_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    // ---- Schemas and ownership ----------------------------------------------------------------

    [Fact]
    public void Parses_create_schema_with_elements()
    {
        const string sql = "create schema s authorization dbo create table t (a int) create view v as select a from t "
            + "grant select on t to u deny delete on t to u";
        var schema = Whole<TsqlCreateSchemaStatement>(sql);
        Assert.Equal(SchemaName, Text(sql, schema.Name));
        Assert.Equal(Dbo, Text(sql, schema.Owner));
        Assert.Equal(4, schema.Elements.Count);
        Assert.IsType<CreateTableStatement>(schema.Elements[0]);
        Assert.IsType<CreateViewStatement>(schema.Elements[1]);
        Assert.IsType<TsqlPermissionStatement>(schema.Elements[TwoItems]);
        Assert.IsType<TsqlPermissionStatement>(schema.Elements[ThreeItems]);
        var ownerOnly = Whole<TsqlCreateSchemaStatement>("create schema authorization dbo");
        Assert.Null(ownerOnly.Name);
        Assert.Empty(ownerOnly.Elements);
        Assert.IsType<SchemaDefinition>(SqlAssert.Parse<SchemaDefinition>("create schema s"));
    }

    [Fact]
    public void Create_schema_stops_at_non_element_statement()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("create schema s select 1");
        Assert.Empty(Assert.IsType<TsqlCreateSchemaStatement>(batch.Statements[0]).Elements);
    }

    [Fact]
    public void Parses_alter_schema_transfer()
    {
        const string sql = "alter schema s transfer object::dbo.t";
        var transfer = Whole<TsqlAlterSchemaTransferStatement>(sql);
        Assert.Equal(SchemaName, Text(sql, transfer.Name));
        Assert.Equal("object", Words(sql, transfer.Securable.ClassKeywords));
        Assert.Equal("dbo t", Words(sql, transfer.Securable.Name));
        const string bareSql = "alter schema s transfer dbo.t";
        Assert.Empty(Whole<TsqlAlterSchemaTransferStatement>(bareSql).Securable.ClassKeywords);
    }

    [Fact]
    public void Parses_alter_authorization()
    {
        const string sql = "alter authorization on xml schema collection::dbo.c to u";
        var alter = Whole<TsqlAlterAuthorizationStatement>(sql);
        Assert.Equal("xml schema collection", Words(sql, alter.Securable.ClassKeywords));
        Assert.Equal(Principal, Text(sql, alter.Principal));
        const string ownerSql = "alter authorization on schema::s to schema owner";
        var owner = Whole<TsqlAlterAuthorizationStatement>(ownerSql);
        Assert.Null(owner.Principal);
        Assert.NotNull(owner.SchemaKeyword);
        Assert.NotNull(owner.OwnerKeyword);
    }

    // ---- Other objects ------------------------------------------------------------------------

    [Fact]
    public void Parses_xml_schema_collections()
    {
        const string sql = "create xml schema collection dbo.c as N'<xsd:schema/>'";
        var create = Whole<TsqlXmlSchemaCollectionStatement>(sql);
        Assert.Equal("dbo c", Words(sql, create.Name));
        Assert.Equal("as", Text(sql, create.ContentKeyword));
        Assert.IsType<LiteralExpression>(create.Content);
        const string alterSql = "alter xml schema collection c add @schema";
        var alter = Whole<TsqlXmlSchemaCollectionStatement>(alterSql);
        Assert.Equal("add", Text(alterSql, alter.ContentKeyword));
        Assert.IsType<EmbeddedHostExpression>(alter.Content);
    }

    [Fact]
    public void Parses_create_assembly()
    {
        const string sql = "create assembly a authorization dbo from 'c:\\a.dll', 'c:\\b.dll' with permission_set = safe";
        var assembly = Whole<TsqlCreateAssemblyStatement>(sql);
        Assert.Equal(Dbo, Text(sql, assembly.Owner));
        Assert.Equal(TwoItems, assembly.Sources.Count);
        Assert.Equal("permission_set", Words(sql, assembly.Option!));
        Assert.Equal("safe", Value(sql, assembly.Option!));
        Assert.Single(Whole<TsqlCreateAssemblyStatement>("create assembly a from 0x4D5A9000").Sources);
    }

    [Fact]
    public void Parses_legacy_defaults_and_rules()
    {
        const string sql = "create rule dbo.r as @range >= 1000 and @range < 20000";
        var rule = Whole<TsqlCreateDefaultOrRuleStatement>(sql);
        Assert.Equal("rule", Text(sql, rule.KindKeyword));
        Assert.Equal("dbo r", Words(sql, rule.Name));
        Assert.IsType<BinaryExpression>(rule.Expression);
        const string defaultSql = "create default d as 0";
        var defaultObject = Whole<TsqlCreateDefaultOrRuleStatement>(defaultSql);
        Assert.Equal("default", Text(defaultSql, defaultObject.KindKeyword));
        Assert.IsType<LiteralExpression>(defaultObject.Expression);
    }

    [Fact]
    public void Parses_external_data_source_file_format_and_model()
    {
        const string sql = "create external data source ds with (location = 'sqlserver://h:1433', credential = c, pushdown = on)";
        var source = Whole<TsqlCreateExternalObjectStatement>(sql);
        Assert.Equal("data source", Words(sql, source.KindKeywords));
        Assert.Equal(ThreeItems, source.Options.Items.Count);
        const string formatSql = "create external file format ff with (format_type = delimitedtext, "
            + "format_options (field_terminator = '|', first_row = 2, use_type_default = true))";
        var format = Whole<TsqlCreateExternalObjectStatement>(formatSql);
        Assert.Equal("file format", Words(formatSql, format.KindKeywords));
        Assert.Equal(ThreeItems, format.Options.Items[^1].Options!.Items.Count);
        const string modelSql = "create external model m authorization dbo with (location = 'https://x', model_type = embeddings)";
        var model = Whole<TsqlCreateExternalObjectStatement>(modelSql);
        Assert.Equal("model", Words(modelSql, model.KindKeywords));
        Assert.Equal(Dbo, Text(modelSql, model.Owner));
    }

    [Fact]
    public void Parses_external_tables_and_cetas()
    {
        const string sql = "create external table dbo.t (a int not null, b varchar(10)) "
            + "with (location = '/t/', data_source = ds, file_format = ff, reject_type = value, reject_value = 0)";
        var table = Whole<TsqlCreateExternalTableStatement>(sql);
        Assert.Equal(TwoItems, table.Elements.Count);
        Assert.Equal(5, table.Options.Items.Count);
        Assert.Null(table.Query);
        const string cetasSql = "create external table t2 with (location = '/x/', data_source = ds, file_format = ff) as select a from t";
        var cetas = Whole<TsqlCreateExternalTableStatement>(cetasSql);
        Assert.Empty(cetas.Elements);
        Assert.IsType<SelectStatement>(cetas.Query);
    }

    [Theory]
    [InlineData("create xml schema collection c as '<x/>'")]
    [InlineData("create assembly a from 'a.dll'")]
    [InlineData("create default d as 0")]
    [InlineData("create external data source ds with (location = 'x')")]
    [InlineData("create external table t (a int) with (location = 'x')")]
    public void Object_syntax_is_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Theory]
    [InlineData("create external data source ds")]
    [InlineData("create external table t (a int)")]
    [InlineData("create assembly a")]
    [InlineData("create rule r")]
    public void Rejects_malformed_objects(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Fact]
    public void Visitor_walks_database_statements()
    {
        var visitor = new SecurityRecorder();
        visitor.Visit(Any("create database d on (name = d, filename = 'f') with trustworthy on"));
        visitor.Visit(Any("create schema s create view v as select a from t"));
        visitor.Visit(Any("create rule r as @v > a"));
        visitor.Visit(Any("create external table t2 with (location = 'x') as select b from t"));
        Assert.Equal(ThreeItems + 1, visitor.Types.Count(type => type == nameof(TsqlObjectOption)));
        Assert.Equal(ThreeItems, visitor.Types.Count(type => type == nameof(IdentifierExpression)));
    }
}
