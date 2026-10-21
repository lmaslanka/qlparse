namespace QlParse.Tests;

// SQL Server indexes and statistics (stream D).
public sealed partial class TsqlDdlTests
{
    [Fact]
    public void Parses_clustered_index_with_include_filter_options_and_storage()
    {
        const string Sql = "create unique clustered index ix on dbo.t (a desc, b) include (c, d) where c is not null "
            + "with (fillfactor = 80, online = on, drop_existing = on, maxdop = 4) on ps (a) filestream_on fs";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal("unique", Text(Sql, index.UniqueKeyword));
        Assert.Equal(Clustered, Text(Sql, index.ClusteredKeyword));
        Assert.Equal("desc", Text(Sql, index.Columns[0].Direction));
        Assert.IsType<IsExpression>(index.Where);
        var extras = index.SqlServer!;
        Assert.Equal("c d", Joined(Sql, extras.Include!.Columns));
        Assert.Equal(Count.Four, extras.Options!.Options.Count);
        Assert.Equal("ps", Text(Sql, extras.Storage!.Target));
        Assert.Equal("fs", Text(Sql, extras.FilestreamOn!.Target));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_nonclustered_index_and_plain_index_without_extras()
    {
        const string Sql = "create nonclustered index ix on t (a)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal(Nonclustered, Text(Sql, index.ClusteredKeyword));
        Assert.Null(index.SqlServer);
        Assert.Null(index.Postgres);
    }

    [Fact]
    public void Parses_index_with_legacy_unparenthesized_options()
    {
        const string Sql = "create index ix on t (a) with pad_index, fillfactor = 70 on [PRIMARY]";
        var extras = ParseWhole<CreateIndexStatement>(Sql).SqlServer!;
        Assert.Null(extras.Options!.OpenParen);
        Assert.Equal(Count.Two, extras.Options.Options.Count);
        Assert.Equal("70", Text(Sql, extras.Options.Options[1].Value[0]));
        Assert.Equal(Primary, Text(Sql, extras.Storage!.Target));
    }

    [Fact]
    public void Parses_indexed_view_index()
    {
        const string Sql = "create unique clustered index ix_v on dbo.v (a, b)";
        var index = ParseWhole<CreateIndexStatement>(Sql);
        Assert.Equal("dbo v", Joined(Sql, index.TableName));
        Assert.Equal(Count.Two, index.Columns.Count);
    }

    [Theory]
    [InlineData("create clustered columnstore index cci on t", "columnstore", "clustered")]
    [InlineData("create nonclustered columnstore index ncci on t (a, b) where a > 0", "columnstore", "nonclustered")]
    [InlineData("create columnstore index ncci on t (a)", "columnstore", "")]
    [InlineData("create spatial index si on t (g)", "spatial", "")]
    [InlineData("create vector index vi on t (v) with (metric = 'cosine', type = 'diskann')", "vector", "")]
    public void Parses_special_indexes(string sql, string expectedKind, string expectedClustered)
    {
        var index = ParseWhole<TsqlCreateSpecialIndexStatement>(sql);
        Assert.Equal(expectedKind, Text(sql, index.KindKeyword));
        Assert.Equal(expectedClustered, Text(sql, index.ClusteredKeyword));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_ordered_clustered_columnstore_index()
    {
        const string Sql = "create clustered columnstore index cci on t order (a, b) with (maxdop = 2, compression_delay = 10 minutes) on fg";
        var index = ParseWhole<TsqlCreateSpecialIndexStatement>(Sql);
        Assert.Null(index.Columns);
        Assert.Equal("a b", Joined(Sql, index.OrderColumns!));
        Assert.Equal("10 minutes", Joined(Sql, index.Options!.Options[1].Value));
        Assert.Equal(FileGroup, Text(Sql, index.Storage!.Target));
    }

    [Fact]
    public void Parses_primary_and_secondary_xml_indexes()
    {
        const string PrimarySql = "create primary xml index px on t (doc)";
        var primary = ParseWhole<TsqlCreateSpecialIndexStatement>(PrimarySql);
        Assert.Equal("primary", Text(PrimarySql, primary.PrimaryKeyword));
        Assert.Equal("xml", Text(PrimarySql, primary.KindKeyword));
        const string Secondary = "create xml index sx on t (doc) using xml index px for path";
        var secondary = ParseWhole<TsqlCreateSpecialIndexStatement>(Secondary);
        Assert.Equal("px", Text(Secondary, secondary.UsingIndexName));
        Assert.Equal("path", Text(Secondary, secondary.SecondaryKind));
        const string Value = "create xml index sx on t (doc) using xml index px for value";
        Assert.Equal("value", Text(Value, ParseWhole<TsqlCreateSpecialIndexStatement>(Value).SecondaryKind));
        SqlAssert.ParseFails(Secondary);
    }

    [Fact]
    public void Parses_spatial_index_with_tessellation_and_nested_options()
    {
        const string Sql = "create spatial index si on t (g) using geometry_grid with (bounding_box = (0, 0, 500, 200), "
            + "grids = (level_1 = medium, level_2 = low), cells_per_object = 64) on fg";
        var index = ParseWhole<TsqlCreateSpecialIndexStatement>(Sql);
        Assert.Equal("geometry_grid", Text(Sql, index.Tessellation));
        var options = index.Options!.Options;
        Assert.Equal(Count.Four, options[0].Options!.Options.Count);
        Assert.Equal("500", Text(Sql, options[0].Options!.Options[Third].Name));
        Assert.Equal("medium", Text(Sql, options[1].Options!.Options[0].Value[0]));
        Assert.Equal(FileGroup, Text(Sql, index.Storage!.Target));
    }

    [Fact]
    public void Parses_json_index_paths()
    {
        const string Sql = "create json index ji on t (doc) for ('$.a', '$.b.c') with (fillfactor = 90)";
        var index = ParseWhole<TsqlCreateSpecialIndexStatement>(Sql);
        Assert.Equal("for", Text(Sql, index.ForKeyword));
        Assert.Equal("'$.a' '$.b.c'", Joined(Sql, index.Paths!));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter index ix on t rebuild partition = 2 with (data_compression = row)", "rebuild")]
    [InlineData("alter index all on dbo.t reorganize with (lob_compaction = on)", "reorganize")]
    [InlineData("alter index ix on t disable", "disable")]
    [InlineData("alter index ix on t resume with (max_duration = 30 minutes)", "resume")]
    [InlineData("alter index ix on t pause", "pause")]
    [InlineData("alter index ix on t abort", "abort")]
    [InlineData("alter index ix on t set (allow_page_locks = on, ignore_dup_key = off)", "set")]
    public void Parses_alter_index_actions(string sql, string expectedAction)
    {
        var statement = ParseWhole<TsqlAlterIndexStatement>(sql);
        Assert.Equal(expectedAction, Text(sql, statement.ActionKeyword));
    }

    [Theory]
    [InlineData("alter index ix on t rebuild partition = 2")]
    [InlineData("alter index all on t reorganize")]
    [InlineData("alter index ix on t set (allow_page_locks = on)")]
    public void Rejects_alter_index_on_table_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Fact]
    public void Parses_alter_index_all_and_online_wait()
    {
        const string Sql = "alter index all on t rebuild with (online = on (wait_at_low_priority (max_duration = 5 minutes, abort_after_wait = blockers)))";
        var statement = ParseWhole<TsqlAlterIndexStatement>(Sql);
        Assert.Equal(All, Text(Sql, statement.AllKeyword));
        Assert.Empty(statement.Name);
        var online = statement.Options!.Options[0];
        Assert.Equal("wait_at_low_priority", Text(Sql, online.Options!.Options[0].Name));
        const string Set = "alter index ix on t set (allow_page_locks = on)";
        Assert.Null(ParseWhole<TsqlAlterIndexStatement>(Set).WithKeyword);
    }

    [Fact]
    public void Parses_drop_index_forms()
    {
        const string Sql = "drop index if exists ix on t with (online = on), dbo.t2.ix2";
        var statement = ParseWhole<TsqlDropIndexStatement>(Sql);
        Assert.NotNull(statement.IfExists);
        Assert.Equal("t", Text(Sql, statement.Items[0].TableName![0]));
        Assert.Equal(Online, Text(Sql, statement.Items[0].Options!.Options[0].Name));
        Assert.Equal("dbo t2 ix2", Joined(Sql, statement.Items[1].Name));
        Assert.Null(statement.Items[1].OnKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_statistics()
    {
        const string Sql = "create statistics s on dbo.t (a, b) where a > 0 with sample 50 percent, norecompute, incremental = on";
        var statement = ParseWhole<TsqlCreateStatisticsStatement>(Sql);
        Assert.Equal("a b", Joined(Sql, statement.Columns));
        Assert.NotNull(statement.Where);
        var options = statement.Options!.Options;
        Assert.Equal("50 percent", Joined(Sql, options[0].Value));
        Assert.Equal("norecompute", Text(Sql, options[1].Name));
        Assert.Equal(On, Text(Sql, options[Third].Value[0]));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("update statistics t", 0)]
    [InlineData("update statistics dbo.t s1 with fullscan", 1)]
    [InlineData("update statistics t (s1, s2) with sample 10 rows, norecompute", 2)]
    [InlineData("update statistics t with resample on partitions (1, 2)", 0)]
    public void Parses_update_statistics(string sql, int expectedStatistics)
    {
        var statement = ParseWhole<TsqlUpdateStatisticsStatement>(sql);
        Assert.Equal(expectedStatistics, statement.Statistics.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Update_statistics_stops_before_the_next_statement()
    {
        const string Sql = "update statistics t select 1";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(Sql);
        Assert.Empty(Assert.IsType<TsqlUpdateStatisticsStatement>(batch.Statements[0]).Statistics);
        Assert.IsType<SelectStatement>(batch.Statements[1]);
    }

    [Fact]
    public void Index_statements_end_without_a_terminator()
    {
        const string Sql = "create index ix on t (a) with (online = on) create index ix2 on t (b) drop index ix on t";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(Sql);
        Assert.Equal(Count.Three, batch.Statements.Count);
        Assert.IsType<TsqlDropIndexStatement>(batch.Statements[Third]);
    }

    [Fact]
    public void Iso_create_index_is_unchanged_without_sql_server()
    {
        var index = SqlAssert.Parse<CreateIndexStatement>("create index ix on t (a, b)");
        Assert.Null(index.SqlServer);
        SqlAssert.ParseFails("create index ix on t (a) include (b)");
        SqlAssert.ParseFails("drop index ix on t with (online = on)");
    }
}
