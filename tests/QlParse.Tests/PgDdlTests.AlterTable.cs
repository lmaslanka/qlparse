namespace QlParse.Tests;

// Postgres ALTER TABLE actions, ALTER COLUMN extensions, and constraint extensions.
public sealed partial class PgDdlTests
{
    private static T ParseAction<T>(string sql) where T : AlterTableAction
    {
        var alter = ParseWhole<AlterTableStatement>(sql);
        return Assert.IsType<T>(alter.Action);
    }

    private static AlterColumnAction ParseColumnAction(string sql) => ParseAction<AlterColumnAction>(sql);

    [Theory]
    [InlineData("alter table t inherit p", "inherit p", 1)]
    [InlineData("alter table t no inherit s.p", "no inherit s.p", 2)]
    public void Parses_inherit_and_no_inherit(string sql, string actionText, int parentParts)
    {
        var action = ParseAction<InheritAction>(sql);
        Assert.Equal(actionText, Text(sql, action.Span));
        Assert.Equal(parentParts, action.Parent.Count);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("alter table t set logged", "logged")]
    [InlineData("alter table t set unlogged", "unlogged")]
    public void Parses_set_logged_and_unlogged(string sql, string persistence)
    {
        var action = ParseAction<SetPersistenceAction>(sql);
        Assert.Equal(persistence, Text(sql, action.Persistence));
        Assert.Equal($"set {persistence}", Text(sql, action.Span));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_column_set_statistics()
    {
        const string Sql = "alter table t alter column a set statistics -1";
        var action = ParseColumnAction(Sql);
        Assert.Equal("statistics", Text(Sql, action.AttributeKeyword));
        Assert.Equal("-1", Text(Sql, action.AttributeValue!.Span));
        Assert.Equal("alter column a set statistics -1", Text(Sql, action.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_set_and_reset_storage_parameters()
    {
        const string Set = "alter table t set (fillfactor = 70, autovacuum_enabled = false)";
        var set = ParseAction<RelationOptionsAction>(Set);
        Assert.Equal(Count.Two, set.Parameters.Parameters.Count);
        Assert.Equal("set (fillfactor = 70, autovacuum_enabled = false)", Text(Set, set.Span));
        SqlAssert.ParseFails(Set);
        const string Reset = "alter table t reset (fillfactor)";
        var reset = ParseAction<RelationOptionsAction>(Reset);
        Assert.Equal("reset", Text(Reset, reset.VerbKeyword));
        Assert.Null(reset.Parameters.Parameters[0].Value);
        SqlAssert.ParseFails(Reset);
    }

    [Fact]
    public void Parses_cluster_on_and_set_without_cluster()
    {
        const string On = "alter table t cluster on i";
        var clusterOn = ParseAction<ClusterAction>(On);
        Assert.Equal(IndexName, Text(On, clusterOn.Index));
        Assert.Equal("cluster on i", Text(On, clusterOn.Span));
        SqlAssert.ParseFails(On);
        const string Without = "alter table t set without cluster";
        var without = ParseAction<ClusterAction>(Without);
        Assert.NotNull(without.WithoutKeyword);
        Assert.Null(without.Index);
        SqlAssert.ParseFails(Without);
    }

    [Theory]
    [InlineData("alter table t alter column a set storage external", "storage", "external")]
    [InlineData("alter table t alter a set storage main", "storage", "main")]
    [InlineData("alter table t alter column a set compression lz4", "compression", "lz4")]
    public void Parses_alter_column_set_storage_and_compression(string sql, string attribute, string value)
    {
        var action = ParseColumnAction(sql);
        Assert.Equal(attribute, Text(sql, action.AttributeKeyword));
        Assert.Equal(value, Text(sql, action.AttributeName));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_set_data_type_using()
    {
        const string Sql = "alter table t alter column a set data type bigint using a::bigint";
        var action = ParseColumnAction(Sql);
        Assert.NotNull(action.DataKeyword);
        Assert.IsType<ColonCastExpression>(action.Using);
        Assert.Equal("a::bigint", Text(Sql, action.Using!.Span));
        Assert.Equal("alter column a set data type bigint using a::bigint", Text(Sql, action.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_column_type_shorthand_with_collation()
    {
        const string Sql = @"alter table t alter column a type text collate ""C"" using a::text";
        var action = ParseColumnAction(Sql);
        Assert.Null(action.SetKeyword);
        Assert.Equal("type", Text(Sql, action.TypeKeyword));
        Assert.Single(action.Collation!);
        Assert.NotNull(action.Using);
        SqlAssert.ParseFails(Sql);
        Assert.NotNull(ParseColumnAction("alter table t alter a type bigint").DataType);
    }

    [Fact]
    public void Iso_set_data_type_is_unchanged()
    {
        var alter = SqlAssert.Parse<AlterTableStatement>("alter table t alter column a set data type bigint");
        var action = Assert.IsType<AlterColumnAction>(alter.Action);
        Assert.NotNull(action.DataType);
        Assert.Null(action.Using);
    }

    [Fact]
    public void Parses_only_star_and_if_exists()
    {
        const string Sql = "alter table if exists only t * drop column if exists b cascade";
        var alter = ParseWhole<AlterTableStatement>(Sql);
        Assert.NotNull(alter.IfExists);
        Assert.Equal("only", Text(Sql, alter.OnlyKeyword));
        Assert.Equal("*", Text(Sql, alter.Star));
        var drop = Assert.IsType<DropColumnAction>(alter.Action);
        Assert.Equal("if exists", Text(Sql, drop.IfExists!.Span));
        Assert.Equal("cascade", Text(Sql, drop.Behavior));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_table_all_in_tablespace()
    {
        const string Sql = "alter table all in tablespace old owned by bob, carol set tablespace fast nowait";
        var alter = ParseWhole<AlterTableStatement>(Sql);
        var all = alter.AllInTablespace!;
        Assert.Equal("old", Text(Sql, all.Tablespace));
        Assert.Equal(Count.Two, all.Roles.Count);
        Assert.Equal("all in tablespace old owned by bob, carol", Text(Sql, all.Span));
        Assert.Empty(alter.Name);
        Assert.IsType<SetTablespaceAction>(alter.Action);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_multiple_actions()
    {
        const string Sql = "alter table t add column if not exists a int, drop column b, alter column c set default 1";
        var alter = ParseWhole<AlterTableStatement>(Sql);
        var add = Assert.IsType<AddColumnAction>(alter.Action);
        Assert.NotNull(add.IfNotExists);
        Assert.Equal(Count.Two, alter.AdditionalActions.Count);
        Assert.Equal("drop column b", Text(Sql, alter.AdditionalActions[0].Span));
        Assert.IsType<AlterColumnAction>(alter.AdditionalActions[1]);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter table t rename to u", null, null)]
    [InlineData("alter table t rename column a to b", "column", "a")]
    [InlineData("alter table t rename a to b", null, "a")]
    [InlineData("alter table t rename constraint c to d", "constraint", "c")]
    public void Parses_rename_actions(string sql, string? targetKeyword, string? target)
    {
        var action = ParseAction<RenameRelationAction>(sql);
        Assert.Equal(targetKeyword, action.TargetKeyword is { } keyword ? Text(sql, keyword) : null);
        Assert.Equal(target, action.Target is { } name ? Text(sql, name) : null);
        Assert.Equal(sql[^1..], Text(sql, action.NewName));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_set_schema_owner_and_tablespace()
    {
        const string Schema = "alter table t set schema s";
        Assert.Equal("set schema s", Text(Schema, ParseAction<SetRelationSchemaAction>(Schema).Span));
        SqlAssert.ParseFails(Schema);
        const string Owner = "alter table t owner to current_user";
        Assert.Equal(SyntaxKind.CurrentUserKeyword, ParseAction<RelationOwnerAction>(Owner).Owner.Kind);
        SqlAssert.ParseFails(Owner);
        const string Tablespace = "alter table t set tablespace fast nowait";
        var action = ParseAction<SetTablespaceAction>(Tablespace);
        Assert.Equal(FastTablespace, Text(Tablespace, action.Tablespace));
        Assert.Equal("set tablespace fast nowait", Text(Tablespace, action.Span));
        SqlAssert.ParseFails(Tablespace);
    }

    [Theory]
    [InlineData("alter table t enable trigger trg", "trg")]
    [InlineData("alter table t disable trigger all", "all")]
    [InlineData("alter table t disable trigger user", "user")]
    [InlineData("alter table t enable replica trigger r1", "r1")]
    [InlineData("alter table t enable always rule r2", "r2")]
    public void Parses_enable_disable_trigger(string sql, string target)
    {
        var action = ParseAction<ToggleTriggerAction>(sql);
        Assert.Equal(target, Text(sql, action.Target));
        Assert.Equal(sql["alter table t ".Length..], Text(sql, action.Span));
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("alter table t replica identity full", "full")]
    [InlineData("alter table t replica identity default", "default")]
    [InlineData("alter table t replica identity nothing", "nothing")]
    [InlineData("alter table t replica identity using index i", "using")]
    public void Parses_replica_identity(string sql, string mode)
    {
        var action = ParseAction<ReplicaIdentityAction>(sql);
        Assert.Equal(mode, Text(sql, action.Mode));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_not_null_and_identity_column_actions()
    {
        Assert.NotNull(ParseColumnAction("alter table t alter column a set not null").NotKeyword);
        Assert.NotNull(ParseColumnAction("alter table t alter column a drop not null").DropKeyword);
        const string Add = "alter table t alter column id add generated always as identity (start with 1 cache 10)";
        var identity = ParseColumnAction(Add).Identity!;
        Assert.Equal(Count.Two, identity.Options.Count);
        Assert.Equal("generated always as identity (start with 1 cache 10)", Text(Add, identity.Span));
        SqlAssert.ParseFails(Add);
        const string DropIdentity = "alter table t alter column a drop identity if exists";
        Assert.NotNull(ParseColumnAction(DropIdentity).IfExists);
        SqlAssert.ParseFails(DropIdentity);
        const string DropExpression = "alter table t alter column a drop expression";
        Assert.NotNull(ParseColumnAction(DropExpression).ExpressionKeyword);
        SqlAssert.ParseFails(DropExpression);
    }

    [Fact]
    public void Parses_alter_column_attribute_options()
    {
        const string Set = "alter table t alter column a set (n_distinct = 100)";
        Assert.Single(ParseColumnAction(Set).Parameters!.Parameters);
        SqlAssert.ParseFails(Set);
        const string Reset = "alter table t alter column a reset (n_distinct)";
        Assert.NotNull(ParseColumnAction(Reset).ResetKeyword);
        SqlAssert.ParseFails(Reset);
    }

    [Fact]
    public void Parses_add_column_generated_identity()
    {
        var add = ParseAction<AddColumnAction>("alter table t add column id int generated by default as identity (start 1 increment 2)");
        Assert.Equal(Count.Two, add.Column.Identity!.Options.Count);
    }

    [Fact]
    public void Parses_attach_partition()
    {
        const string Sql = "alter table t attach partition p1 for values in (1)";
        var action = ParseAction<AttachPartitionAction>(Sql);
        Assert.Equal("p1", Text(Sql, action.Name[0]));
        Assert.Single(action.Bound!.Values);
        Assert.Equal("attach partition p1 for values in (1)", Text(Sql, action.Span));
        SqlAssert.ParseFails(Sql);
        Assert.NotNull(ParseAction<AttachPartitionAction>("alter table t attach partition p2 default").Bound!.DefaultKeyword);
    }

    [Theory]
    [InlineData("alter table t detach partition p1", null)]
    [InlineData("alter table t detach partition p1 concurrently", "concurrently")]
    [InlineData("alter table t detach partition p1 finalize", "finalize")]
    public void Parses_detach_partition(string sql, string? mode)
    {
        var action = ParseAction<DetachPartitionAction>(sql);
        Assert.Equal(mode, action.Mode is { } token ? Text(sql, token) : null);
        SqlAssert.ParseFails(sql);
    }

    // ---- Constraints ----

    [Fact]
    public void Parses_exclusion_constraint()
    {
        const string Sql = "create table t (r tsrange, room int, exclude using gist (room with =, r with &&) include (x) where (room > 0))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var constraint = Assert.IsType<TableConstraint>(table.Elements![Count.Two]);
        var exclusion = constraint.Exclusion!;
        Assert.Equal("gist", Text(Sql, exclusion.Method));
        Assert.Equal(Count.Two, exclusion.Elements.Count);
        Assert.Equal(SyntaxKind.EqualsToken, exclusion.Elements[0].Operator.Kind);
        Assert.Equal("&&", Text(Sql, exclusion.Elements[1].Operator));
        Assert.Equal("r with &&", Text(Sql, exclusion.Elements[1].Span));
        Assert.Single(exclusion.IndexParameters!.Include!);
        Assert.IsType<BinaryExpression>(exclusion.Where);
        Assert.Equal(Text(Sql, constraint.Span), Text(Sql, exclusion.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_named_exclusion_constraint_with_characteristics()
    {
        const string Sql = "alter table t add constraint no_overlap exclude using gist (r with &&) deferrable initially deferred";
        var constraint = ParseAction<AddConstraintAction>(Sql).Constraint;
        Assert.NotNull(constraint.Exclusion);
        Assert.Equal("deferred", Text(Sql, constraint.InitiallyWhen));
        SqlAssert.ParseFails(Sql);
        Assert.NotNull(ParseAction<AddConstraintAction>("alter table t add exclude (r with &&)").Constraint.Exclusion);
    }

    [Fact]
    public void Parses_add_constraint_not_valid()
    {
        const string Sql = "alter table t add constraint fk foreign key (a) references p (id) not valid";
        var constraint = ParseAction<AddConstraintAction>(Sql).Constraint;
        Assert.Equal("valid", Text(Sql, constraint.ValidKeyword));
        Assert.Equal("constraint fk foreign key (a) references p (id) not valid", Text(Sql, constraint.Span));
        SqlAssert.ParseFails(Sql);
        Assert.NotNull(ParseAction<AddConstraintAction>("alter table t add check (a > 0) not valid").Constraint.ValidNotKeyword);
    }

    [Fact]
    public void Parses_validate_constraint()
    {
        const string Sql = "alter table t validate constraint c";
        var action = ParseAction<ValidateTableConstraintAction>(Sql);
        Assert.Equal("c", Text(Sql, action.Name));
        Assert.Equal("validate constraint c", Text(Sql, action.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_check_no_inherit()
    {
        const string Sql = "create table t (a int check (a > 0) no inherit, check (a < 9) no inherit)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var column = Assert.IsType<ColumnDefinition>(table.Elements![0]);
        var columnCheck = Assert.Single(column.Constraints);
        Assert.Equal("check (a > 0) no inherit", Text(Sql, columnCheck.Span));
        var tableCheck = Assert.IsType<TableConstraint>(table.Elements[1]);
        Assert.NotNull(tableCheck.InheritNoKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_foreign_key_set_null_columns_and_match_full()
    {
        const string Sql = "create table t (a int references p (id) match full on delete set null (a) deferrable initially deferred)";
        var column = Assert.IsType<ColumnDefinition>(ParseWhole<CreateTableStatement>(Sql).Elements![0]);
        var constraint = Assert.Single(column.Constraints);
        Assert.Equal("full", Text(Sql, constraint.MatchType));
        var action = Assert.Single(constraint.Actions);
        Assert.Single(action.Columns!);
        Assert.Equal("on delete set null (a)", Text(Sql, action.Span));
        Assert.NotNull(constraint.DeferrableKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_unique_nulls_not_distinct_and_index_parameters()
    {
        const string Sql = "create table t (a int unique nulls not distinct, primary key (a) include (b) with (fillfactor = 90) using index tablespace fast)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var unique = Assert.Single(Assert.IsType<ColumnDefinition>(table.Elements![0]).Constraints);
        Assert.NotNull(unique.NullsNotKeyword);
        var primary = Assert.IsType<TableConstraint>(table.Elements[1]);
        var parameters = primary.IndexParameters!;
        Assert.Single(parameters.Include!);
        Assert.Single(parameters.Parameters!.Parameters);
        Assert.Equal(FastTablespace, Text(Sql, parameters.Tablespace));
        Assert.Equal("include (b) with (fillfactor = 90) using index tablespace fast", Text(Sql, parameters.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_column_primary_key_using_index_tablespace()
    {
        const string Sql = "create table t (a int primary key using index tablespace fast)";
        var column = Assert.IsType<ColumnDefinition>(ParseWhole<CreateTableStatement>(Sql).Elements![0]);
        var constraint = Assert.Single(column.Constraints);
        Assert.Equal("index", Text(Sql, constraint.IndexParameters!.IndexKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter table t add constraint pk primary key using index i")]
    [InlineData("alter table t add constraint u unique using index i deferrable")]
    public void Parses_add_constraint_using_index(string sql)
    {
        var constraint = ParseAction<AddConstraintAction>(sql).Constraint;
        Assert.Equal(IndexName, Text(sql, constraint.IndexName));
        Assert.Null(constraint.Columns);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Iso_constraints_keep_their_shape()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("create table t (a int, unique (a) deferrable, check (a > 0))");
        var unique = Assert.IsType<TableConstraint>(table.Elements![1]);
        Assert.Null(unique.IndexParameters);
        Assert.Null(unique.ValidKeyword);
        Assert.NotNull(unique.DeferrableKeyword);
    }

    private const string VisitorAlterSql =
        "alter table t alter column a type int using u1 + 1, add exclude using gist (x1 with &&) where (w1 > 0), attach partition p for values in (v1)";

    [Fact]
    public void Visitor_reaches_alter_table_expressions()
    {
        var visitor = new IdentifierCollector(VisitorAlterSql);
        visitor.Visit(SqlAssert.ParsePg<AlterTableStatement>(VisitorAlterSql));
        Assert.Equal(["u1", "x1", "w1", "v1"], visitor.Names);
    }
}
