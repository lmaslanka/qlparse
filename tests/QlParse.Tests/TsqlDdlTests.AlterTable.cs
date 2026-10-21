namespace QlParse.Tests;

// SQL Server ALTER TABLE (stream D).
public sealed partial class TsqlDdlTests
{
    private static T AlterAction<T>(string sql) where T : AlterTableAction =>
        Assert.IsType<T>(ParseWhole<AlterTableStatement>(sql).Action);

    [Fact]
    public void Parses_alter_column_type_nullability_and_online()
    {
        const string Sql = "alter table t alter column c varchar(20) collate Latin1_General_CI_AS not null with (online = on)";
        var action = AlterAction<TsqlAlterColumnAction>(Sql);
        Assert.Equal(ColumnC, Text(Sql, action.Name));
        Assert.Equal("varchar", Text(Sql, action.DataType!.Name));
        Assert.Equal("Latin1_General_CI_AS", Text(Sql, action.Collation![0]));
        Assert.Equal("not", Text(Sql, action.NotKeyword));
        Assert.Equal(Online, Text(Sql, action.Options!.Options[0].Name));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_column_sparse_null()
    {
        const string Sql = "alter table t alter column c int sparse null";
        var action = AlterAction<TsqlAlterColumnAction>(Sql);
        Assert.Equal("sparse", Text(Sql, action.SparseKeyword));
        Assert.Equal(Null, Text(Sql, action.NullKeyword));
        Assert.Null(action.NotKeyword);
    }

    [Theory]
    [InlineData("alter table t alter column c add rowguidcol", "add", "rowguidcol")]
    [InlineData("alter table t alter column c drop persisted", "drop", "persisted")]
    [InlineData("alter table t alter column c add not for replication", "add", "not for replication")]
    [InlineData("alter table t alter column c drop hidden", "drop", "hidden")]
    [InlineData("alter table t alter column c add sparse", "add", "sparse")]
    public void Parses_alter_column_add_drop_property(string sql, string expectedVerb, string expectedProperty)
    {
        var action = AlterAction<TsqlAlterColumnAction>(sql);
        Assert.Equal(expectedVerb, Text(sql, action.VerbKeyword));
        Assert.Equal(expectedProperty, Joined(sql, action.Property));
        Assert.Null(action.DataType);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_column_add_and_drop_masked()
    {
        const string Add = "alter table t alter column email add masked with (function = 'email()')";
        Assert.Equal("'email()'", Text(Add, AlterAction<TsqlAlterColumnAction>(Add).Masking!.Function));
        const string Drop = "alter table t alter column email drop masked";
        var drop = AlterAction<TsqlAlterColumnAction>(Drop);
        Assert.Equal("drop", Text(Drop, drop.VerbKeyword));
        Assert.Null(drop.Masking!.WithKeyword);
        Assert.Empty(drop.Property);
        SqlAssert.ParseFails(Add);
    }

    [Fact]
    public void Parses_single_add_column_without_column_keyword_as_iso_action()
    {
        const string Sql = "alter table t add c int identity(1, 1) not null";
        var action = AlterAction<AddColumnAction>(Sql);
        Assert.Null(action.ColumnKeyword);
        Assert.NotNull(action.Column.SqlServer!.Identity);
    }

    [Fact]
    public void Parses_multiple_add_elements()
    {
        const string Sql = "alter table t add c int null, d varchar(10) constraint df_d default 'x', constraint ck check (c > 0), index ix (c)";
        var action = AlterAction<TsqlAddElementsAction>(Sql);
        Assert.Equal(Count.Four, action.Elements.Count);
        Assert.IsType<ColumnDefinition>(action.Elements[0]);
        Assert.Equal("df_d", Text(Sql, Assert.IsType<ColumnDefinition>(action.Elements[1]).SqlServer!.DefaultConstraintName));
        Assert.IsType<TableConstraint>(action.Elements[Third]);
        Assert.IsType<TsqlTableIndex>(action.Elements[Fourth]);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_add_default_constraint_for_column()
    {
        const string Sql = "alter table t add constraint df_c default (0) for c with values";
        var constraint = AlterAction<AddConstraintAction>(Sql).Constraint;
        Assert.Equal("df_c", Text(Sql, constraint.ConstraintName));
        Assert.IsType<ParenExpression>(constraint.SqlServer!.Default!.Value);
        Assert.Equal(ColumnC, Text(Sql, constraint.SqlServer.ForColumn));
        Assert.Equal("values", Text(Sql, constraint.SqlServer.ValuesKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter table t with nocheck add constraint fk foreign key (a) references r (id)", "nocheck")]
    [InlineData("alter table t with check add constraint ck check (a > 0)", "check")]
    public void Parses_with_check_prefix_on_add_constraint(string sql, string expectedMode)
    {
        var statement = ParseWhole<AlterTableStatement>(sql);
        Assert.Equal(With, Text(sql, statement.SqlServer!.WithKeyword));
        Assert.Equal(expectedMode, Text(sql, statement.SqlServer.CheckMode));
        Assert.IsType<AddConstraintAction>(statement.Action);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_add_period_for_system_time()
    {
        const string Sql = "alter table t add period for system_time (vf, vt)";
        Assert.Equal("vt", Text(Sql, AlterAction<AddPeriodAction>(Sql).Period.EndColumn));
    }

    [Fact]
    public void Parses_drop_lists()
    {
        const string Sql = "alter table t drop constraint if exists ck, pk with (online = on), column c, d";
        var elements = AlterAction<TsqlDropElementsAction>(Sql).Elements;
        Assert.Equal(Count.Four, elements.Count);
        Assert.Equal("constraint", Text(Sql, elements[0].KindKeyword));
        Assert.NotNull(elements[0].IfExists);
        Assert.Null(elements[1].KindKeyword);
        Assert.Equal(Online, Text(Sql, elements[1].Options!.Options[0].Name));
        Assert.Equal("column", Text(Sql, elements[Third].KindKeyword));
        Assert.Equal("d", Text(Sql, elements[Fourth].Name));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_single_drops_as_iso_actions()
    {
        const string Column = "alter table t drop column if exists c";
        var column = AlterAction<DropColumnAction>(Column);
        Assert.NotNull(column.IfExists);
        Assert.Null(column.Behavior);
        const string Constraint = "alter table t drop constraint ck";
        Assert.Equal("ck", Text(Constraint, AlterAction<DropConstraintAction>(Constraint).Name));
        SqlAssert.ParseFails(Column);
        SqlAssert.ParseFails(Constraint);
    }

    [Fact]
    public void Parses_drop_constraint_without_keyword_and_with_move_to()
    {
        const string Sql = "alter table t drop pk with (move to fg)";
        var element = Assert.Single(AlterAction<TsqlDropElementsAction>(Sql).Elements);
        Assert.Null(element.KindKeyword);
        Assert.Equal("to fg", Joined(Sql, element.Options!.Options[0].Value));
    }

    [Fact]
    public void Parses_drop_period_for_system_time()
    {
        const string Sql = "alter table t drop period for system_time";
        var element = Assert.Single(AlterAction<TsqlDropElementsAction>(Sql).Elements);
        Assert.Equal("period", Text(Sql, element.KindKeyword));
        Assert.Equal("system_time", Text(Sql, element.Name));
    }

    [Fact]
    public void Parses_check_and_nocheck_constraint()
    {
        const string AllSql = "alter table t with check check constraint all";
        var statement = ParseWhole<AlterTableStatement>(AllSql);
        var action = Assert.IsType<TsqlCheckConstraintAction>(statement.Action);
        Assert.Equal(All, Text(AllSql, action.AllKeyword));
        Assert.Equal("check", Text(AllSql, statement.SqlServer!.CheckMode));
        const string Some = "alter table t nocheck constraint ck1, ck2";
        var nocheck = AlterAction<TsqlCheckConstraintAction>(Some);
        Assert.Equal("nocheck", Text(Some, nocheck.CheckKeyword));
        Assert.Equal("ck1 ck2", Joined(Some, nocheck.Names));
        SqlAssert.ParseFails(Some);
    }

    [Theory]
    [InlineData("alter table t enable trigger all", "enable", "")]
    [InlineData("alter table t disable trigger tr1, tr2", "disable", "tr1 tr2")]
    public void Parses_enable_disable_trigger(string sql, string expectedVerb, string expectedNames)
    {
        var action = AlterAction<TsqlToggleTriggersAction>(sql);
        Assert.Equal(expectedVerb, Text(sql, action.VerbKeyword));
        Assert.Equal(expectedNames, Joined(sql, action.Names));
        Assert.Equal(expectedNames.Length == 0, action.AllKeyword is not null);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_switch_partition()
    {
        const string Sql = "alter table t switch partition $partition.pf(5) to dbo.t2 partition 3 with (wait_at_low_priority (max_duration = 1 minutes, abort_after_wait = self))";
        var action = AlterAction<TsqlSwitchPartitionAction>(Sql);
        Assert.IsType<TsqlPartitionFunctionCallExpression>(action.SourcePartition);
        Assert.Equal("dbo t2", Joined(Sql, action.Target));
        Assert.IsType<LiteralExpression>(action.TargetPartition);
        var wait = action.Options!.Options[0];
        Assert.Equal("wait_at_low_priority", Text(Sql, wait.Name));
        Assert.Equal("1 minutes", Joined(Sql, wait.Options!.Options[0].Value));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_switch_without_partitions()
    {
        const string Sql = "alter table t switch to t2";
        var action = AlterAction<TsqlSwitchPartitionAction>(Sql);
        Assert.Null(action.PartitionKeyword);
        Assert.Null(action.TargetPartition);
    }

    [Theory]
    [InlineData("alter table t set (lock_escalation = auto)", "lock_escalation")]
    [InlineData("alter table t set (system_versioning = off)", "system_versioning")]
    public void Parses_set_table_options(string sql, string expectedOption)
    {
        var action = AlterAction<TsqlSetTableOptionsAction>(sql);
        Assert.Equal(expectedOption, Text(sql, action.Options.Options[0].Name));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_rebuild()
    {
        const string Sql = "alter table t rebuild partition = all with (data_compression = page on partitions (1 to 4))";
        var action = AlterAction<TsqlRebuildAction>(Sql);
        Assert.Equal(All, Text(Sql, action.Partition));
        Assert.NotNull(action.Options!.Options[0].Partitions);
        const string Bare = "alter table t rebuild";
        Assert.Null(AlterAction<TsqlRebuildAction>(Bare).Partition);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Iso_alter_table_is_unchanged_without_sql_server()
    {
        var statement = SqlAssert.Parse<AlterTableStatement>("alter table t add column c integer");
        Assert.Null(statement.SqlServer);
        Assert.NotNull(Assert.IsType<AddColumnAction>(statement.Action).ColumnKeyword);
        SqlAssert.ParseFails("alter table t alter column c int");
    }
}
