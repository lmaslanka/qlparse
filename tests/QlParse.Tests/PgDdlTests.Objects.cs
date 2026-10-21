namespace QlParse.Tests;

// Postgres indexes, views, materialized views, rules, sequences, tablespaces, and REINDEX.
public sealed partial class PgDdlTests
{
    private const string IndexName = "i";
    private const string ConcurrentlyWord = "concurrently";

    // ---- Indexes ----

    [Fact]
    public void Parses_create_index_using_method()
    {
        const string Sql = "create index i on t using gin (a)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal("gin", Text(Sql, index.Postgres!.Method));
        Assert.Equal("using", Text(Sql, index.Postgres!.UsingKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("btree")]
    [InlineData("hash")]
    [InlineData("gist")]
    [InlineData("spgist")]
    [InlineData("brin")]
    public void Parses_every_index_method(string method)
    {
        var sql = $"create index i on t using {method} (a)";
        Assert.Equal(method, Text(sql, ParseWhole<CreateIndexStatement>(sql).Postgres!.Method));
    }

    [Fact]
    public void Parses_create_index_concurrently_if_not_exists_on_only()
    {
        const string Sql = "create index concurrently if not exists i on only t (a)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal(ConcurrentlyWord, Text(Sql, index.Postgres!.ConcurrentlyKeyword));
        Assert.NotNull(index.Postgres!.IfNotExists);
        Assert.Equal("only", Text(Sql, index.Postgres!.OnlyKeyword));
        Assert.Equal(IndexName, Text(Sql, index.Name[0]));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_index_without_name()
    {
        const string Sql = "create index concurrently on t (a)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Empty(index.Name);
        Assert.NotNull(index.Postgres!.ConcurrentlyKeyword);
        Assert.Empty(ParseWhole<CreateIndexStatement>("create unique index on t (a)").Name);
        SqlAssert.ParseFails("create index on t (a)");
    }

    [Fact]
    public void Iso_index_named_concurrently_is_unchanged()
    {
        var index = SqlAssert.Parse<CreateIndexStatement>("create index concurrently on t (a)");
        Assert.Single(index.Name);
        Assert.Null(index.Postgres?.ConcurrentlyKeyword);
    }

    [Fact]
    public void Parses_create_index_include()
    {
        const string Sql = "create unique index i on t (a) include (b, c)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal(Count.Two, index.Postgres!.Options!.Include!.Count);
        Assert.Equal("include (b, c)", Text(Sql, index.Postgres!.Options.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create index i on t using gin (a jsonb_path_ops)", "jsonb_path_ops")]
    [InlineData("create index i on t using gin (a gin_trgm_ops)", "gin_trgm_ops")]
    [InlineData("create index i on t (a public.text_pattern_ops desc)", "text_pattern_ops")]
    public void Parses_operator_classes(string sql, string operatorClass)
    {
        var column = Assert.Single(ParseWhole<CreateIndexStatement>(sql).Columns);
        Assert.Equal(operatorClass, Text(sql, column.Postgres!.OperatorClass![^1]));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_operator_class_parameters_collation_and_null_order()
    {
        const string Sql = @"create index i on t using gist (a collate ""C"" gist_trgm_ops(siglen=32) desc nulls first)";
        var column = Assert.Single(ParseWhole<CreateIndexStatement>(Sql).Columns);
        Assert.IsType<CollateExpression>(column.Expression);
        Assert.Equal("siglen=32", Text(Sql, Assert.Single(column.Postgres!.OperatorClassParameters!.Parameters).Span));
        Assert.Equal("first", Text(Sql, column.NullsOrder));
        Assert.Equal(@"a collate ""C"" gist_trgm_ops(siglen=32) desc nulls first", Text(Sql, column.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_index_storage_tablespace_and_nulls_distinct()
    {
        const string Sql = "create unique index i on t (a) nulls not distinct with (fillfactor = 70) tablespace fast where a > 0";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        var options = index.Postgres!.Options!;
        Assert.NotNull(options.NullsNotKeyword);
        Assert.Single(options.Parameters!.Parameters);
        Assert.Equal(FastTablespace, Text(Sql, options.Tablespace));
        Assert.NotNull(index.Where);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_drop_index_concurrently()
    {
        const string Sql = "drop index concurrently if exists i, j cascade";
        var drop = ParseWhole<DropIndexStatement>(Sql);
        Assert.Equal(ConcurrentlyWord, Text(Sql, drop.Postgres!.ConcurrentlyKeyword));
        Assert.Single(drop.Postgres!.AdditionalNames);
        Assert.NotNull(drop.Behavior);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("reindex table t", "table", null)]
    [InlineData("reindex index concurrently i", "index", "concurrently")]
    [InlineData("reindex schema s", "schema", null)]
    [InlineData("reindex database d", "database", null)]
    [InlineData("reindex system", "system", null)]
    public void Parses_reindex(string sql, string expectedKind, string? expectedMode)
    {
        var reindex = ParseWhole<ReindexStatement>(sql);
        Assert.Equal(expectedKind, Text(sql, reindex.Kind));
        Assert.Equal(expectedMode, reindex.ConcurrentlyKeyword is { } token ? Text(sql, token) : null);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_reindex_options()
    {
        const string Sql = "reindex (verbose, tablespace fast) index i";
        var reindex = ParseWhole<ReindexStatement>(Sql);
        var options = reindex.Options!.Parameters;
        Assert.Equal(Count.Two, options.Count);
        Assert.Equal(FastTablespace, Text(Sql, options[1].Value));
        Assert.Equal(IndexName, Text(Sql, reindex.Name[0]));
    }

    [Theory]
    [InlineData("alter index if exists i set tablespace fast", typeof(SetTablespaceAction))]
    [InlineData("alter index i set schema s", typeof(SetRelationSchemaAction))]
    [InlineData("alter index i set (fillfactor = 50)", typeof(RelationOptionsAction))]
    [InlineData("alter index i reset (fillfactor)", typeof(RelationOptionsAction))]
    [InlineData("alter index i attach partition p_i", typeof(AttachPartitionAction))]
    [InlineData("alter index i alter column 1 set statistics 100", typeof(AlterColumnAction))]
    public void Parses_alter_index_actions(string sql, Type actionType)
    {
        var alter = ParseWhole<AlterIndexStatement>(sql);
        Assert.IsType(actionType, alter.Postgres!.RelationAction);
        SqlAssert.ParseFails(sql);
    }

    // ---- Views ----

    [Fact]
    public void Parses_create_or_replace_view()
    {
        const string Sql = "create or replace view v as select 1";
        var view = ParseWhole<CreateViewStatement>(Sql);
        Assert.Equal("or", Text(Sql, view.Postgres!.OrKeyword));
        Assert.Equal("replace", Text(Sql, view.Postgres!.ReplaceKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create or replace temp view v as select 1", "temp")]
    [InlineData("create temporary recursive view v (n) as select 1", "temporary")]
    [InlineData("create temp view v as select 1", "temp")]
    public void Parses_temporary_views(string sql, string expectedPersistence)
    {
        Assert.Equal(expectedPersistence, Text(sql, ParseWhole<CreateViewStatement>(sql).Postgres!.TemporaryKeyword));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_view_options_and_check_option()
    {
        const string Sql = "create view v (a, b) with (security_barrier, check_option = local) as select 1, 2 with cascaded check option";
        var view = ParseWhole<CreateViewStatement>(Sql);
        Assert.Equal(Count.Two, view.Columns!.Count);
        Assert.Equal(Count.Two, view.Postgres!.Options!.Parameters.Count);
        Assert.Equal("cascaded", Text(Sql, view.Levels));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter view v rename to w", typeof(RenameRelationAction))]
    [InlineData("alter view if exists v set schema s", typeof(SetRelationSchemaAction))]
    [InlineData("alter view v owner to bob", typeof(RelationOwnerAction))]
    [InlineData("alter view v set (security_barrier = true)", typeof(RelationOptionsAction))]
    public void Parses_alter_view_actions(string sql, Type actionType)
    {
        var alter = ParseWhole<AlterViewStatement>(sql);
        var action = Assert.IsType<AlterViewRelationAction>(alter.Action);
        Assert.IsType(actionType, action.Action);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_create_materialized_view()
    {
        const string Sql = "create materialized view if not exists mv (a) using heap with (fillfactor = 70) tablespace fast as select 1 with no data";
        var view = ParseWhole<CreateMaterializedViewStatement>(Sql);
        Assert.NotNull(view.IfNotExists);
        Assert.Single(view.Columns!);
        Assert.Equal("heap", Text(Sql, view.Method));
        Assert.Single(view.Options!.Parameters);
        Assert.Equal(FastTablespace, Text(Sql, view.Tablespace));
        Assert.IsType<SelectStatement>(view.Query);
        Assert.NotNull(view.NoKeyword);
        SqlAssert.ParseFails(Sql);
        Assert.Null(ParseWhole<CreateMaterializedViewStatement>("create materialized view mv as select 1").DataKeyword);
    }

    [Fact]
    public void Parses_drop_and_alter_materialized_view()
    {
        const string Drop = "drop materialized view if exists mv, mv2 cascade";
        var drop = ParseWhole<DropViewStatement>(Drop);
        Assert.Equal("materialized", Text(Drop, drop.Postgres!.MaterializedKeyword));
        Assert.Single(drop.Postgres!.AdditionalNames);
        SqlAssert.ParseFails(Drop);
        const string Alter = "alter materialized view if exists mv set tablespace fast";
        var alter = ParseWhole<AlterViewStatement>(Alter);
        Assert.NotNull(alter.Postgres!.MaterializedKeyword);
        Assert.IsType<SetTablespaceAction>(Assert.IsType<AlterViewRelationAction>(alter.Action).Action);
        SqlAssert.ParseFails(Alter);
        Assert.IsType<AlterViewColumnAction>(ParseWhole<AlterViewStatement>("alter materialized view mv alter column a set statistics 10").Action);
    }

    [Theory]
    [InlineData("refresh materialized view mv", null, null)]
    [InlineData("refresh materialized view concurrently mv with data", "concurrently", null)]
    [InlineData("refresh materialized view mv with no data", null, "no")]
    public void Parses_refresh_materialized_view(string sql, string? expectedMode, string? expectedNo)
    {
        var refresh = ParseWhole<RefreshMaterializedViewStatement>(sql);
        Assert.Equal(expectedMode, refresh.ConcurrentlyKeyword is { } token ? Text(sql, token) : null);
        Assert.Equal(expectedNo, refresh.NoKeyword is { } noToken ? Text(sql, noToken) : null);
        SqlAssert.ParseFails(sql);
    }

    // ---- Rules ----

    private const string MultiCommandRule =
        "create rule r as on delete to t do (delete from u where id = old.id;"
        + " insert into log values (1))";

    [Fact]
    public void Parses_create_rule_instead_nothing()
    {
        const string Sql = "create rule r as on insert to t do instead nothing";
        var rule = ParseWhole<CreateRuleStatement>(Sql);
        Assert.Equal("insert", Text(Sql, rule.Event));
        Assert.Equal("instead", Text(Sql, rule.Kind));
        Assert.NotNull(rule.NothingKeyword);
        Assert.Empty(rule.Commands);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_or_replace_rule_with_condition()
    {
        const string Sql = "create or replace rule r as on update to s.t where new.a > 0 do also update u set a = 1";
        var rule = ParseWhole<CreateRuleStatement>(Sql);
        Assert.NotNull(rule.ReplaceKeyword);
        Assert.IsType<BinaryExpression>(rule.Where);
        Assert.IsType<UpdateStatement>(Assert.Single(rule.Commands));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_rule_command_list()
    {
        var rule = ParseWhole<CreateRuleStatement>(MultiCommandRule);
        Assert.Equal(Count.Two, rule.Commands.Count);
        Assert.Single(rule.Semicolons);
        Assert.IsType<DeleteStatement>(rule.Commands[0]);
        Assert.IsType<InsertStatement>(rule.Commands[1]);
    }

    [Fact]
    public void Parses_drop_rule()
    {
        const string Sql = "drop rule if exists r on t cascade";
        var drop = ParseWhole<DropRuleStatement>(Sql);
        Assert.NotNull(drop.IfExists);
        Assert.Equal(TableName, Text(Sql, drop.Table[0]));
        Assert.Null(ParseWhole<DropRuleStatement>("drop rule r on t").Behavior);
        SqlAssert.ParseFails(Sql);
    }

    // ---- Sequences ----

    [Theory]
    [InlineData("alter sequence s owned by t.id", Count.Two)]
    [InlineData("alter sequence if exists s owned by none", 1)]
    public void Parses_alter_sequence_owned_by(string sql, int targetParts)
    {
        var alter = ParseWhole<AlterSequenceStatement>(sql);
        var option = Assert.Single(alter.Options);
        Assert.Equal("owned", Text(sql, option.Name));
        Assert.Equal(targetParts, option.Postgres!.Target!.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_sequence_options()
    {
        const string Sql = "alter sequence s as bigint increment by 2 minvalue 1 no maxvalue cache 5 cycle restart with 10";
        var alter = ParseWhole<AlterSequenceStatement>(Sql);
        Assert.NotNull(alter.DataType);
        Assert.Equal(Count.Six, alter.Options.Count);
        Assert.Equal("restart with 10", Text(Sql, alter.Options[^1].Span));
        Assert.Null(Assert.Single(ParseWhole<AlterSequenceStatement>("alter sequence s restart").Options).Value);
    }

    [Theory]
    [InlineData("alter sequence s rename to s2", typeof(RenameRelationAction))]
    [InlineData("alter sequence s set schema x", typeof(SetRelationSchemaAction))]
    [InlineData("alter sequence s owner to bob", typeof(RelationOwnerAction))]
    [InlineData("alter sequence s set logged", typeof(SetPersistenceAction))]
    public void Parses_alter_sequence_actions(string sql, Type actionType)
    {
        Assert.IsType(actionType, ParseWhole<AlterSequenceStatement>(sql).Action);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_create_sequence_postgres_options()
    {
        const string Sql = "create temp sequence if not exists s start 1 increment 2 cache 10 no cycle owned by t.id";
        var sequence = ParseWhole<CreateSequenceStatement>(Sql);
        Assert.Equal("temp", Text(Sql, sequence.Postgres!.PersistenceKeyword));
        Assert.NotNull(sequence.Postgres!.IfNotExists);
        Assert.Equal(Count.Five, sequence.Options.Count);
        Assert.Null(sequence.Options[0].WithKeyword);
        Assert.Equal("owned by t.id", Text(Sql, sequence.Options[^1].Span));
        SqlAssert.ParseFails(Sql);
        Assert.NotNull(ParseWhole<CreateSequenceStatement>("create unlogged sequence s").Postgres!.PersistenceKeyword);
    }

    // ---- Tablespaces ----

    [Fact]
    public void Parses_create_tablespace()
    {
        const string Sql = "create tablespace fast owner bob location '/ssd' with (random_page_cost = 1.1)";
        var tablespace = ParseWhole<CreateTablespaceStatement>(Sql);
        Assert.Equal(FastTablespace, Text(Sql, tablespace.Name));
        Assert.Equal("bob", Text(Sql, tablespace.Owner));
        Assert.Equal("'/ssd'", Text(Sql, tablespace.Location));
        Assert.Single(tablespace.Options!.Parameters);
        SqlAssert.ParseFails(Sql);
        Assert.Null(ParseWhole<CreateTablespaceStatement>("create tablespace fast location '/x'").Owner);
    }

    [Theory]
    [InlineData("drop tablespace fast", false)]
    [InlineData("drop tablespace if exists fast", true)]
    public void Parses_drop_tablespace(string sql, bool ifExists)
    {
        var drop = ParseWhole<DropTablespaceStatement>(sql);
        Assert.Equal(ifExists, drop.IfExists is not null);
        Assert.Equal(FastTablespace, Text(sql, drop.Name));
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("alter tablespace fast rename to faster", typeof(RenameRelationAction))]
    [InlineData("alter tablespace fast owner to bob", typeof(RelationOwnerAction))]
    [InlineData("alter tablespace fast set (random_page_cost = 1.1)", typeof(RelationOptionsAction))]
    [InlineData("alter tablespace fast reset (random_page_cost)", typeof(RelationOptionsAction))]
    public void Parses_alter_tablespace(string sql, Type actionType)
    {
        Assert.IsType(actionType, ParseWhole<AlterTablespaceStatement>(sql).Action);
        SqlAssert.ParseFails(sql);
    }

    // ---- Visitor ----

    private const string VisitorRuleSql =
        "create rule r as on update to t where w1 > 0 do also (update u set a = s1 where k1 = 1)";

    [Fact]
    public void Visitor_walks_postgres_object_statements()
    {
        var visitor = new IdentifierCollector(VisitorRuleSql);
        visitor.Visit(SqlAssert.ParsePg<CreateRuleStatement>(VisitorRuleSql));
        Assert.Equal(["w1", "s1", "k1"], visitor.Names);
        var walker = new SqlVisitor();
        walker.Visit(SqlAssert.ParsePg<CreateMaterializedViewStatement>("create materialized view mv as select a from t"));
        walker.Visit(SqlAssert.ParsePg<RefreshMaterializedViewStatement>("refresh materialized view mv"));
        walker.Visit(SqlAssert.ParsePg<ReindexStatement>("reindex table t"));
        walker.Visit(SqlAssert.ParsePg<CreateTablespaceStatement>("create tablespace x location '/x'"));
        walker.Visit(SqlAssert.ParsePg<DropTablespaceStatement>("drop tablespace x"));
        walker.Visit(SqlAssert.ParsePg<AlterTablespaceStatement>("alter tablespace x rename to y"));
        walker.Visit(SqlAssert.ParsePg<DropRuleStatement>("drop rule r on t"));
        walker.Visit(SqlAssert.ParsePg<AlterSequenceStatement>("alter sequence s as int owned by none"));
        walker.Visit(SqlAssert.ParsePg<AlterViewStatement>("alter view v owner to bob"));
        walker.Visit(SqlAssert.ParsePg<AlterIndexStatement>("alter index i set tablespace x"));
    }
}
