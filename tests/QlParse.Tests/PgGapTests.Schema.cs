namespace QlParse.Tests;

// Postgres gaps in DDL (workstream D) and schema-object (workstream E) statements.
public sealed partial class PgGapTests
{
    private const string CollatedIndexSql = """create index i on t (lower(a) collate "C" text_pattern_ops)""";

    // ---- 16. ALTER TABLE actions ----

    [Fact]
    public void Parses_alter_constraint_characteristics()
    {
        const string sql = "alter table t alter constraint c not deferrable initially immediate";
        var action = Assert.IsType<AlterConstraintAction>(WalkPg<AlterTableStatement>(sql).Action);
        AssertSpan(sql, action.Span, "alter constraint c not deferrable initially immediate");
        Assert.NotNull(action.DeferrableNotKeyword);
        AssertSpan(sql, Assert.NotNull(action.InitiallyWhen).Span, "immediate");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_set_access_method()
    {
        const string sql = "alter table t set access method heap";
        var action = Assert.IsType<SetAccessMethodAction>(WalkPg<AlterTableStatement>(sql).Action);
        AssertSpan(sql, action.Span, "set access method heap");
        AssertSpan(sql, action.Method.Span, "heap");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_of_type_and_not_of()
    {
        const string ofSql = "alter table t of s.mytype";
        var of = Assert.IsType<TypedTableAction>(WalkPg<AlterTableStatement>(ofSql).Action);
        AssertSpan(ofSql, of.Span, "of s.mytype");
        Assert.Equal(Count.Two, of.TypeName.Count);
        SqlAssert.ParseFails(ofSql);

        const string notOfSql = "alter table t not of";
        var notOf = Assert.IsType<TypedTableAction>(WalkPg<AlterTableStatement>(notOfSql).Action);
        AssertSpan(notOfSql, notOf.Span, "not of");
        Assert.Empty(notOf.TypeName);
    }

    [Fact]
    public void Parses_set_without_oids()
    {
        const string sql = "alter table t set without oids, set access method heap";
        var alter = WalkPg<AlterTableStatement>(sql);
        AssertSpan(sql, Assert.IsType<SetWithoutOidsAction>(alter.Action).Span, "set without oids");
        Assert.IsType<SetAccessMethodAction>(Assert.Single(alter.AdditionalActions));
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("""alter table t add column c text collate "C" """)]
    [InlineData("alter table t alter column c drop default")]
    [InlineData("alter table t alter column c set default 1")]
    public void Parses_existing_column_actions_under_postgres(string sql)
    {
        var alter = SqlAssert.ParsePg<AlterTableStatement>(sql);
        Assert.Equal(sql.TrimEnd().Length, alter.Span.End);
    }

    // ---- 17. column STORAGE and COMPRESSION ----

    [Fact]
    public void Parses_column_storage_and_compression()
    {
        const string sql = "create table t (a text storage main compression pglz not null, b text compression lz4)";
        var table = WalkPg<CreateTableStatement>(sql);
        var first = Assert.IsType<ColumnDefinition>(table.Elements![0]);
        AssertSpan(sql, Assert.NotNull(first.Postgres?.StorageMode).Span, "main");
        AssertSpan(sql, Assert.NotNull(first.Postgres?.CompressionMethod).Span, "pglz");
        AssertSpan(sql, first.Span, "a text storage main compression pglz not null");
        var second = Assert.IsType<ColumnDefinition>(table.Elements[1]);
        Assert.Null(second.Postgres?.StorageKeyword);
        AssertSpan(sql, second.Span, "b text compression lz4");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_added_column_with_storage()
    {
        const string sql = "alter table t add column c text storage external";
        var add = Assert.IsType<AddColumnAction>(WalkPg<AlterTableStatement>(sql).Action);
        AssertSpan(sql, add.Span, "add column c text storage external");
    }

    // ---- 18. EXCLUDE ... WITH OPERATOR(schema.op) ----

    [Fact]
    public void Parses_qualified_exclusion_operator()
    {
        const string sql = "create table t (r tsrange, exclude using gist (r with operator(pg_catalog.&&)))";
        var table = WalkPg<CreateTableStatement>(sql);
        var constraint = Assert.IsType<TableConstraint>(table.Elements![1]);
        var element = Assert.Single(constraint.Exclusion!.Elements);
        AssertSpan(sql, element.Span, "r with operator(pg_catalog.&&)");
        AssertSpan(sql, element.Operator.Span, "&&");
        Assert.Single(element.QualifiedOperator!.SchemaParts);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_qualified_exclusion_operator_in_alter_table()
    {
        const string sql = "alter table t add constraint c exclude using gist (r with operator(public.&&), p with =)";
        var add = Assert.IsType<AddConstraintAction>(WalkPg<AlterTableStatement>(sql).Action);
        var elements = add.Constraint.Exclusion!.Elements;
        Assert.NotNull(elements[0].QualifiedOperator);
        Assert.Null(elements[1].QualifiedOperator);
    }

    // ---- 19. ALTER INDEX / MATERIALIZED VIEW ALL IN TABLESPACE ----

    [Fact]
    public void Parses_alter_index_all_in_tablespace()
    {
        const string sql = "alter index all in tablespace x set tablespace y";
        var alter = WalkPg<AlterIndexStatement>(sql);
        Assert.Empty(alter.Name);
        AssertSpan(sql, alter.AllInTablespace!.Span, "all in tablespace x");
        AssertSpan(sql, Assert.IsType<SetTablespaceAction>(alter.RelationAction).Span, "set tablespace y");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_materialized_view_all_in_tablespace()
    {
        const string sql = "alter materialized view all in tablespace x owned by r set tablespace y nowait";
        var alter = WalkPg<AlterViewStatement>(sql);
        Assert.NotNull(alter.MaterializedKeyword);
        AssertSpan(sql, alter.AllInTablespace!.Span, "all in tablespace x owned by r");
        var action = Assert.IsType<AlterViewRelationAction>(alter.Action);
        AssertSpan(sql, action.Span, "set tablespace y nowait");
        SqlAssert.ParseFails(sql);
    }

    // ---- 20. typed tables and LIKE with several options ----

    [Fact]
    public void Parses_typed_table()
    {
        const string sql = "create table t of mytype (a with options not null, primary key (a))";
        var table = WalkPg<CreateTableStatement>(sql);
        AssertSpan(sql, table.TypeName![0].Span, "mytype");
        Assert.Equal(Count.Two, table.Elements!.Count);
    }

    [Fact]
    public void Parses_like_with_several_options()
    {
        const string sql = "create table t (like u including defaults excluding indexes)";
        var like = Assert.IsType<LikeClause>(Assert.Single(WalkPg<CreateTableStatement>(sql).Elements!));
        Assert.Equal(Count.Two, like.Options.Count);
        AssertSpan(sql, like.Options[1].Span, "excluding indexes");
        SqlAssert.ParseFails(sql);
    }

    // ---- 21. index expression with COLLATE and an operator class ----

    [Fact]
    public void Parses_index_expression_with_collation_and_opclass()
    {
        var index = WalkPg<CreateIndexStatement>(CollatedIndexSql);
        var column = Assert.Single(index.Columns);
        Assert.IsType<CollateExpression>(column.Expression);
        AssertSpan(CollatedIndexSql, column.OperatorClass![0].Span, "text_pattern_ops");
        SqlAssert.ParseFails(CollatedIndexSql);
    }

    // ---- 22. schema statements ----

    [Fact]
    public void Parses_drop_table_if_exists_cascade()
    {
        const string sql = "drop table if exists a cascade";
        var drop = WalkPg<DropTableStatement>(sql);
        AssertSpan(sql, drop.IfExists!.Span, "if exists");
        AssertSpan(sql, Assert.NotNull(drop.Behavior).Span, CascadeText);
    }

    [Fact]
    public void Parses_drop_schema_list()
    {
        const string sql = "drop schema if exists a, b cascade";
        var drop = WalkPg<DropSchemaStatement>(sql);
        AssertSpan(sql, drop.IfExists!.Span, "if exists");
        AssertSpan(sql, Assert.Single(drop.AdditionalNames)[0].Span, "b");
        AssertSpan(sql, Assert.NotNull(drop.Behavior).Span, CascadeText);
        SqlAssert.ParseFails(sql);
        Assert.Null(WalkPg<DropSchemaStatement>("drop schema a").Behavior);
    }

    [Fact]
    public void Parses_create_schema_if_not_exists()
    {
        const string sql = "create schema if not exists s authorization r";
        var schema = WalkPg<SchemaDefinition>(sql);
        AssertSpan(sql, schema.IfNotExists!.Span, "if not exists");
        AssertSpan(sql, Assert.NotNull(schema.Authorization).Span, RoleText);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_schema_owner_and_rename()
    {
        const string sql = "alter schema s owner to r";
        var owner = WalkPg<AlterSchemaStatement>(sql);
        Assert.Null(owner.RenameKeyword);
        Assert.Empty(owner.NewName);
        AssertSpan(sql, Assert.NotNull(owner.Owner).Span, RoleText);
        SqlAssert.ParseFails(sql);

        Assert.Single(WalkPg<AlterSchemaStatement>("alter schema s rename to t").NewName);
    }

    // ---- 23. ALTER FOREIGN TABLE column actions ----

    [Theory]
    [InlineData("alter foreign table f add column c int", "add column c int")]
    [InlineData("alter foreign table f drop column c", "drop column c")]
    [InlineData("alter foreign table f alter column c type text", "alter column c type text")]
    [InlineData("alter foreign table f alter column c set default 1", "alter column c set default 1")]
    [InlineData("alter foreign table f alter column c drop default", "alter column c drop default")]
    [InlineData("alter foreign table f alter column c set not null", "alter column c set not null")]
    [InlineData("alter foreign table f alter column c drop not null", "alter column c drop not null")]
    public void Parses_alter_foreign_table_column_actions(string sql, string actionText)
    {
        var action = Assert.IsType<PgForeignTableAction>(Assert.Single(WalkPg<PgAlterStatement>(sql).Actions));
        AssertSpan(sql, action.Span, actionText);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_foreign_table_column_options_among_actions()
    {
        const string sql = "alter foreign table f alter column c options (add x 'y'), owner to r";
        var alter = WalkPg<PgAlterStatement>(sql);
        Assert.Equal(Count.Two, alter.Actions.Count);
        var column = Assert.IsType<AlterColumnAction>(Assert.IsType<PgForeignTableAction>(alter.Actions[0]).Action);
        AssertSpan(sql, column.Options!.Span, "options (add x 'y')");
        Assert.IsType<PgOwnerAction>(alter.Actions[1]);
    }

    // ---- 24. foreign table CHECK constraints ----

    [Fact]
    public void Parses_foreign_table_check_constraints()
    {
        const string sql = "create foreign table f (a int check (a > 0), constraint c check (a < 10)) server s";
        var table = WalkPg<PgCreateForeignTableStatement>(sql);
        var column = Assert.Single(table.Columns);
        AssertSpan(sql, Assert.Single(column.Checks).Span, "check (a > 0)");
        AssertSpan(sql, column.Span, "a int check (a > 0)");
        var constraint = Assert.Single(table.Constraints);
        AssertSpan(sql, constraint.Span, "constraint c check (a < 10)");
        SqlAssert.ParseFails(sql);
    }

    // ---- 25. ALTER EXTENSION TRANSFORM / MATERIALIZED VIEW members ----

    [Fact]
    public void Parses_extension_transform_member()
    {
        const string sql = "alter extension e add transform for int language plpgsql";
        var member = Assert.IsType<PgMemberAction>(Assert.Single(WalkPg<PgAlterStatement>(sql).Actions));
        AssertSpan(sql, member.Member.Span, "for int language plpgsql");
        AssertSpan(sql, Assert.Single(member.Member.Arguments).Span, "int");
        AssertSpan(sql, Assert.NotNull(member.Member.Language).Span, "plpgsql");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_extension_materialized_view_member()
    {
        const string sql = "alter extension e drop materialized view s.mv";
        var member = Assert.IsType<PgMemberAction>(Assert.Single(WalkPg<PgAlterStatement>(sql).Actions));
        Assert.Equal(Count.Two, member.ObjectKind.Count);
        AssertSpan(sql, member.Member.Span, "s.mv");
        SqlAssert.ParseFails(sql);
    }

    // ---- 26. Postgres ALTER DOMAIN actions ----

    [Theory]
    [InlineData("alter domain d set not null", "set not null")]
    [InlineData("alter domain d drop not null", "drop not null")]
    [InlineData("alter domain d set schema s", "set schema s")]
    [InlineData("alter domain d rename constraint a to b", "rename constraint a to b")]
    [InlineData("alter domain d validate constraint c", "validate constraint c")]
    [InlineData("alter domain d owner to r", "owner to r")]
    [InlineData("alter domain d rename to e", "rename to e")]
    public void Parses_postgres_alter_domain_actions(string sql, string actionText)
    {
        var action = Assert.IsType<PgDomainAction>(WalkPg<AlterDomainStatement>(sql).Action);
        AssertSpan(sql, action.Span, actionText);
    }

    [Fact]
    public void Parses_alter_domain_add_constraint_not_valid()
    {
        const string sql = "alter domain d add constraint c check (value > 0) not valid";
        var add = Assert.IsType<AddDomainConstraintAction>(WalkPg<AlterDomainStatement>(sql).Action);
        AssertSpan(sql, Assert.NotNull(add.ValidKeyword).Span, "valid");
        AssertSpan(sql, add.Constraint.Span, "constraint c check (value > 0)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_domain_drop_constraint_if_exists()
    {
        const string sql = "alter domain d drop constraint if exists c cascade";
        var drop = Assert.IsType<DropDomainConstraintAction>(WalkPg<AlterDomainStatement>(sql).Action);
        AssertSpan(sql, drop.IfExists!.Span, "if exists");
        AssertSpan(sql, Assert.NotNull(drop.Behavior).Span, CascadeText);
        SqlAssert.ParseFails(sql);
        Assert.Null(Assert.IsType<DropDomainConstraintAction>(WalkPg<AlterDomainStatement>("alter domain d drop constraint c").Action).Behavior);
    }

    [Fact]
    public void Alter_domain_set_default_keeps_the_iso_node()
    {
        const string sql = "alter domain d set default 1";
        Assert.IsType<SetDomainDefaultAction>(WalkPg<AlterDomainStatement>(sql).Action);
    }

    [Theory]
    [InlineData("alter domain d drop not null")]
    [InlineData("alter domain d rename constraint a to b")]
    [InlineData("alter domain d owner to r")]
    public void Postgres_alter_domain_actions_fail_under_iso(string sql) => SqlAssert.ParseFails(sql);

    // ---- 27. the generic ALTER is lenient about which shared action an object kind takes ----

    [Fact]
    public void Generic_alter_accepts_any_shared_action_for_a_claimed_kind()
    {
        // A language has no schema, but the parser is syntax-only and does not police this.
        const string sql = "alter language plpgsql set schema s";
        var action = Assert.IsType<PgSetSchemaAction>(Assert.Single(WalkPg<PgAlterStatement>(sql).Actions));
        AssertSpan(sql, action.Span, "set schema s");
    }
}
