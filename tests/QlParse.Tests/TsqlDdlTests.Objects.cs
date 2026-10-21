namespace QlParse.Tests;

// SQL Server views, synonyms, partitioning, CREATE TYPE and multi-name DROP (stream D).
public sealed partial class TsqlDdlTests
{
    // ---- Views ----

    [Fact]
    public void Parses_view_attributes()
    {
        const string Sql = "create view dbo.v (x, y) with schemabinding, encryption, view_metadata as select a, b from dbo.t with check option";
        var view = ParseWhole<CreateViewStatement>(Sql);
        Assert.Equal("x y", Joined(Sql, view.Columns!));
        Assert.Equal("schemabinding encryption view_metadata", Joined(Sql, view.SqlServer!.Attributes));
        Assert.Equal(With, Text(Sql, view.SqlServer.AttributesWithKeyword));
        Assert.Equal("option", Text(Sql, view.OptionKeyword));
        Assert.IsType<SelectStatement>(view.Query);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Plain_view_has_no_extras()
    {
        const string Sql = "create view v as select 1 as a";
        Assert.Null(ParseWhole<CreateViewStatement>(Sql).SqlServer);
    }

    [Fact]
    public void Parses_create_or_alter_view_with_attributes()
    {
        const string Sql = "create or alter view v with schemabinding as select a from dbo.t";
        var statement = ParseWhole<TsqlCreateOrAlterViewStatement>(Sql);
        Assert.Equal("alter", Text(Sql, statement.View.CreateKeyword));
        Assert.Equal("schemabinding", Text(Sql, statement.View.SqlServer!.Attributes[0]));
        SqlAssert.ParseFails(Sql);
    }

    // ---- Synonyms ----

    [Theory]
    [InlineData("create synonym s for srv.db.dbo.t", "s", "srv db dbo t")]
    [InlineData("create synonym dbo.s for t", "dbo s", "t")]
    public void Parses_create_synonym(string sql, string expectedName, string expectedTarget)
    {
        var synonym = ParseWhole<TsqlCreateSynonymStatement>(sql);
        Assert.Equal(expectedName, Joined(sql, synonym.Name));
        Assert.Equal(expectedTarget, Joined(sql, synonym.Target));
        SqlAssert.ParseFails(sql);
    }

    // ---- Partitioning ----

    [Fact]
    public void Parses_create_partition_function()
    {
        const string Sql = "create partition function pf (datetime2(0)) as range right for values ('2020-01-01', '2021-01-01', '2022-01-01')";
        var function = ParseWhole<TsqlCreatePartitionFunctionStatement>(Sql);
        Assert.Equal(Pf, Text(Sql, function.Name));
        Assert.Equal("datetime2", Text(Sql, function.ParameterType.Name));
        Assert.Equal("right", Text(Sql, function.Boundary));
        Assert.Equal(Count.Three, function.Values.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_function_without_boundary_side_or_values()
    {
        const string Sql = "create partition function pf (int) as range for values ()";
        var function = ParseWhole<TsqlCreatePartitionFunctionStatement>(Sql);
        Assert.Null(function.Boundary);
        Assert.Empty(function.Values);
    }

    [Theory]
    [InlineData("create partition scheme ps as partition pf to (fg1, fg2, [PRIMARY])", 3, false)]
    [InlineData("create partition scheme ps as partition pf all to ([PRIMARY])", 1, true)]
    public void Parses_create_partition_scheme(string sql, int expectedFileGroups, bool expectedAll)
    {
        var scheme = ParseWhole<TsqlCreatePartitionSchemeStatement>(sql);
        Assert.Equal(Pf, Text(sql, scheme.FunctionName));
        Assert.Equal(expectedFileGroups, scheme.FileGroups.Count);
        Assert.Equal(expectedAll, scheme.AllKeyword is not null);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("alter partition function pf () split range (500)", "split")]
    [InlineData("alter partition function pf () merge range ('2020-01-01')", "merge")]
    public void Parses_alter_partition_function(string sql, string expectedAction)
    {
        var statement = ParseWhole<TsqlAlterPartitionFunctionStatement>(sql);
        Assert.Equal(expectedAction, Text(sql, statement.ActionKeyword));
        Assert.IsType<LiteralExpression>(statement.Value);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("alter partition scheme ps next used fg3", "fg3")]
    [InlineData("alter partition scheme ps next used", "")]
    public void Parses_alter_partition_scheme(string sql, string expectedFileGroup)
    {
        var statement = ParseWhole<TsqlAlterPartitionSchemeStatement>(sql);
        Assert.Equal(expectedFileGroup, Text(sql, statement.FileGroup));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Next_used_without_filegroup_stops_before_the_next_statement()
    {
        const string Sql = "alter partition scheme ps next used create table t (a int)";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(Sql);
        Assert.Null(Assert.IsType<TsqlAlterPartitionSchemeStatement>(batch.Statements[0]).FileGroup);
        Assert.IsType<CreateTableStatement>(batch.Statements[1]);
    }

    // ---- Types ----

    [Theory]
    [InlineData("create type dbo.ssn from varchar(11) not null", "varchar", "not")]
    [InlineData("create type t from int null", "int", "")]
    [InlineData("create type t from decimal(10, 2)", "decimal", "")]
    public void Parses_alias_types(string sql, string expectedBase, string expectedNot)
    {
        var type = ParseWhole<TsqlCreateTypeStatement>(sql);
        Assert.Equal(expectedBase, Text(sql, type.BaseType!.Name));
        Assert.Equal(expectedNot, Text(sql, type.NotKeyword));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_table_type()
    {
        const string Sql = "create type dbo.tt as table (id int not null primary key nonclustered, b varchar(10) index ix_b, index ix (id, b)) with (memory_optimized = on)";
        var type = ParseWhole<TsqlCreateTypeStatement>(Sql);
        Assert.Equal("table", Text(Sql, type.TableKeyword));
        Assert.Equal(Count.Three, type.Elements!.Count);
        Assert.IsType<TsqlTableIndex>(type.Elements[Third]);
        Assert.Equal("memory_optimized", Text(Sql, type.Options!.Options[0].Name));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_clr_type()
    {
        const string Sql = "create type dbo.Point external name Geo.[Geo.Point]";
        var type = ParseWhole<TsqlCreateTypeStatement>(Sql);
        Assert.Equal("Geo [Geo.Point]", Joined(Sql, type.ExternalName!));
        Assert.Null(type.BaseType);
        SqlAssert.ParseFails(Sql);
    }

    // ---- DROP ----

    [Theory]
    [InlineData("drop table if exists a, dbo.b, c", "table", 3, true)]
    [InlineData("drop view v1, v2", "view", 2, false)]
    [InlineData("drop procedure if exists p", "procedure", 1, true)]
    [InlineData("drop proc p1, p2", "proc", 2, false)]
    [InlineData("drop function if exists f1, f2", "function", 2, true)]
    [InlineData("drop sequence s", "sequence", 1, false)]
    [InlineData("drop synonym if exists s", "synonym", 1, true)]
    [InlineData("drop type dbo.t", "type", 1, false)]
    [InlineData("drop statistics t.s1, t.s2", "statistics", 2, false)]
    [InlineData("drop partition function pf", "partition function", 1, false)]
    [InlineData("drop partition scheme if exists ps", "partition scheme", 1, true)]
    public void Parses_multi_name_drop(string sql, string expectedKind, int expectedNames, bool expectedIfExists)
    {
        var drop = ParseWhole<TsqlDropObjectsStatement>(sql);
        Assert.Equal(expectedKind, Joined(sql, drop.ObjectKind));
        Assert.Equal(expectedNames, drop.Names.Count);
        Assert.Equal(expectedIfExists, drop.IfExists is not null);
    }

    [Theory]
    [InlineData("drop trigger if exists tr on database", "database")]
    [InlineData("drop trigger tr1, tr2 on all server", "all server")]
    [InlineData("drop trigger tr", "")]
    public void Parses_drop_trigger_scopes(string sql, string expectedScope) =>
        Assert.Equal(expectedScope, Joined(sql, ParseWhole<TsqlDropObjectsStatement>(sql).Scope));

    [Theory]
    [InlineData("drop table if exists t")]
    [InlineData("drop table a, b")]
    [InlineData("drop view v1, v2")]
    [InlineData("drop synonym s")]
    [InlineData("drop partition function pf")]
    public void Rejects_tsql_drop_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Fact]
    public void Leaves_security_object_drops_to_other_parsers()
    {
        Assert.IsNotType<TsqlDropObjectsStatement>(Sql.Parse("drop login l", SqlOptions.SqlServer).Root);
        Assert.IsNotType<TsqlDropObjectsStatement>(Sql.Parse("drop schema if exists s", SqlOptions.SqlServer).Root);
    }
}
