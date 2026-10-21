namespace QlParse.Tests;

// Postgres schema objects (stream E): types / domains / enums, extensions and languages, foreign data,
// operator / aggregate / cast / collation definitions, full text search, replication, and triggers.
public sealed class PgSchemaTests
{
    private const int OneItem = 1;
    private const int TwoItems = 2;
    private const int ThreeItems = 3;
    private const int FourItems = 4;
    private const int FiveItems = 5;
    private const int SixItems = 6;
    private const int Third = 2;
    private const int Fourth = 3;
    private const int Fifth = 4;
    private const int Sixth = 5;
    private const int Seventh = 6;
    private const string Cascade = "cascade";
    private const string NameS = "s";
    private const string NameT = "t";

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Text(string sql, SyntaxToken token) => Text(sql, token.Span);

    private static string Text(string sql, SyntaxToken? token) => token is { } value ? Text(sql, value) : string.Empty;

    private static T ParseWhole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParsePg<T>(sql);
        Assert.Equal(sql, Text(sql, node.Span));
        new SqlVisitor().Visit(node);
        return node;
    }

    private static T Action<T>(string sql) where T : PgAlterAction =>
        Assert.IsType<T>(Assert.Single(ParseWhole<PgAlterStatement>(sql).Actions));

    // ---- Types, domains, and enums ----

    [Fact]
    public void Parses_create_type_as_enum()
    {
        const string Sql = "create type mood as enum ('sad', 'ok', 'happy')";
        var type = ParseWhole<PgCreateTypeStatement>(Sql);
        Assert.Equal("enum", Text(Sql, type.KindKeyword));
        Assert.Equal(ThreeItems, type.Labels.Count);
        Assert.Equal("'happy'", Text(Sql, type.Labels[^1]));
        Assert.Null(type.Definition);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_empty_enum()
    {
        const string Sql = "create type e as enum ()";
        Assert.Empty(ParseWhole<PgCreateTypeStatement>(Sql).Labels);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_type_as_range()
    {
        const string Sql = "create type floatrange as range (subtype = float8, subtype_opclass = float8_ops, collation = \"C\", "
            + "canonical = f, subtype_diff = float8mi, multirange_type_name = floatmulti)";
        var type = ParseWhole<PgCreateTypeStatement>(Sql);
        Assert.Equal("range", Text(Sql, type.KindKeyword));
        var definition = Assert.IsType<PgDefinitionList>(type.Definition);
        Assert.Equal(SixItems, definition.Elements.Count);
        Assert.Equal("subtype = float8", Text(Sql, definition.Elements[0].Span));
        Assert.Equal("float8", Text(Sql, definition.Elements[0].Type!.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_shell_and_base_types()
    {
        const string Shell = "create type box2";
        var shell = ParseWhole<PgCreateTypeStatement>(Shell);
        Assert.Null(shell.AsKeyword);
        Assert.Null(shell.Definition);
        SqlAssert.ParseFails(Shell);

        const string Base = "create type box (internallength = 16, input = box_in, output = box_out, alignment = double, "
            + "like = int4, category = 'U', passedbyvalue)";
        var baseType = ParseWhole<PgCreateTypeStatement>(Base);
        var elements = baseType.Definition!.Elements;
        Assert.Equal("16", Text(Base, elements[0].Literal));
        Assert.Equal("like", Text(Base, elements[Fifth].Name));
        Assert.Equal("'U'", Text(Base, elements[Sixth].Literal));
        Assert.Null(elements[^1].EqualsToken);
        SqlAssert.ParseFails(Base);
    }

    [Fact]
    public void Iso_composite_type_keeps_iso_node_under_postgres()
    {
        Assert.NotNull(SqlAssert.ParsePg<CreateTypeStatement>("create type c as (a int, b text)").Attributes);
        Assert.NotNull(SqlAssert.ParsePg<CreateTypeStatement>("create type d as int").Representation);
    }

    [Fact]
    public void Parses_alter_type_add_value()
    {
        const string Sql = "alter type mood add value if not exists 'meh' before 'ok'";
        var add = Action<PgAddEnumValueAction>(Sql);
        Assert.NotNull(add.NotKeyword);
        Assert.Equal("'meh'", Text(Sql, add.Value));
        Assert.Equal("before", Text(Sql, add.Placement));
        Assert.Equal("add value if not exists 'meh' before 'ok'", Text(Sql, add.Span));
        Assert.Null(Action<PgAddEnumValueAction>("alter type mood add value 'x'").Placement);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_type_rename_value()
    {
        const string Sql = "alter type mood rename value 'sad' to 'blue'";
        var rename = Action<PgRenameAction>(Sql);
        Assert.Equal("value", Text(Sql, rename.Kind));
        Assert.Equal("'sad'", Text(Sql, rename.OldName));
        Assert.Equal("'blue'", Text(Sql, rename.NewName));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_type_common_actions()
    {
        Assert.Null(Action<PgRenameAction>("alter type t rename to t2").OldName);
        Assert.Equal(SyntaxKind.CurrentUserKeyword, Action<PgOwnerAction>("alter type t owner to current_user").Owner.Kind);
        const string Schema = "alter type t set schema s";
        Assert.Equal(NameS, Text(Schema, Action<PgSetSchemaAction>(Schema).Schema));
        const string Attribute = "alter type t rename attribute a to b cascade";
        var rename = Action<PgRenameAction>(Attribute);
        Assert.Equal("attribute", Text(Attribute, rename.Kind));
        Assert.Equal(Cascade, Text(Attribute, rename.Behavior));
        SqlAssert.ParseFails(Schema);
    }

    [Fact]
    public void Parses_alter_type_attribute_actions()
    {
        const string Sql = "alter type t add attribute c int collate \"C\" cascade, drop attribute if exists d, "
            + "alter attribute e set data type text";
        var alter = ParseWhole<PgAlterStatement>(Sql);
        Assert.Equal(ThreeItems, alter.Actions.Count);
        var add = Assert.IsType<PgAttributeAction>(alter.Actions[0]);
        Assert.Equal("add attribute c int collate \"C\" cascade", Text(Sql, add.Span));
        Assert.NotNull(add.Collation);
        var drop = Assert.IsType<PgAttributeAction>(alter.Actions[1]);
        Assert.NotNull(drop.ExistsKeyword);
        Assert.Null(drop.Type);
        var change = Assert.IsType<PgAttributeAction>(alter.Actions[Third]);
        Assert.NotNull(change.DataKeyword);
        Assert.Equal("text", Text(Sql, change.Type!.Span));
    }

    [Fact]
    public void Parses_drop_type_postgres_forms()
    {
        const string Sql = "drop type if exists a, sch.b cascade";
        var drop = ParseWhole<PgDropStatement>(Sql);
        Assert.Equal("type", Text(Sql, Assert.Single(drop.ObjectKind)));
        Assert.NotNull(drop.ExistsKeyword);
        Assert.Equal(TwoItems, drop.Objects.Count);
        Assert.Equal("sch.b", Text(Sql, drop.Objects[1].Span));
        Assert.Equal(Cascade, Text(Sql, drop.Behavior));
        Assert.Null(ParseWhole<PgDropStatement>("drop type t").Behavior);
        Assert.IsType<DropTypeStatement>(SqlAssert.ParsePg<DropTypeStatement>("drop type sch.person restrict"));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_domain_with_null_constraints()
    {
        const string Sql = "create domain posint as int default 1 constraint nn not null check (value > 0) null";
        var domain = ParseWhole<CreateDomainStatement>(Sql);
        Assert.Single(domain.Constraints);
        Assert.Equal(TwoItems, domain.NullConstraints.Count);
        Assert.Equal("constraint nn not null", Text(Sql, domain.NullConstraints[0].Span));
        Assert.Null(domain.NullConstraints[1].NotKeyword);
        Assert.NotNull(domain.Default);
        SqlAssert.ParseFails(Sql);
        Assert.Empty(SqlAssert.Parse<CreateDomainStatement>("create domain d int check (value > 0)").NullConstraints);
    }

    [Fact]
    public void Parses_drop_domain_postgres_forms()
    {
        const string Sql = "drop domain if exists d1, d2";
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>(Sql).Objects.Count);
        Assert.IsType<DropDomainStatement>(SqlAssert.ParsePg<DropDomainStatement>("drop domain d1 cascade"));
        SqlAssert.ParseFails(Sql);
    }

    // ---- Extensions and languages ----

    [Fact]
    public void Parses_create_extension()
    {
        const string Sql = "create extension if not exists \"uuid-ossp\" with schema public version '1.1' cascade";
        var created = ParseWhole<PgCreateExtensionStatement>(Sql);
        Assert.Equal("\"uuid-ossp\"", Text(Sql, created.Name));
        Assert.NotNull(created.NotKeyword);
        Assert.NotNull(created.WithKeyword);
        Assert.Equal("public", Text(Sql, created.Schema));
        Assert.Equal("'1.1'", Text(Sql, created.Version));
        Assert.NotNull(created.CascadeKeyword);
        Assert.Null(ParseWhole<PgCreateExtensionStatement>("create extension hstore").Schema);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_extension()
    {
        const string Update = "alter extension hstore update to '2.0'";
        var update = Action<PgKeywordAction>(Update);
        Assert.Equal(TwoItems, update.Keywords.Count);
        Assert.Equal("'2.0'", Text(Update, Assert.Single(update.Values)));
        Assert.Empty(Action<PgKeywordAction>("alter extension hstore update").Values);
        Assert.IsType<PgSetSchemaAction>(Action<PgSetSchemaAction>("alter extension e set schema s"));

        const string Add = "alter extension e add function f(int, text)";
        var add = Action<PgMemberAction>(Add);
        Assert.Equal("function", Text(Add, Assert.Single(add.ObjectKind)));
        Assert.Equal(TwoItems, add.Member.Arguments.Count);
        Assert.Equal("f(int, text)", Text(Add, add.Member.Span));
        Assert.Equal("drop", Text("alter extension e drop table t", Action<PgMemberAction>("alter extension e drop table t").Action));
        Assert.NotNull(Action<PgMemberAction>("alter extension e add operator class c using btree").Member.AccessMethod);
        Assert.NotNull(Action<PgMemberAction>("alter extension e add cast (int as text)").Member.AsKeyword);
        SqlAssert.ParseFails(Add);
    }

    [Fact]
    public void Parses_drop_extension()
    {
        const string Sql = "drop extension if exists a, b cascade";
        var drop = ParseWhole<PgDropStatement>(Sql);
        Assert.Equal("extension", Text(Sql, drop.ObjectKind[0]));
        Assert.Equal(TwoItems, drop.Objects.Count);
        Assert.Equal(Cascade, Text(Sql, drop.Behavior));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_language()
    {
        const string Sql = "create or replace trusted procedural language plsample handler plsample_call_handler "
            + "inline plsample_inline validator plsample_validator";
        var language = ParseWhole<PgCreateLanguageStatement>(Sql);
        Assert.NotNull(language.ReplaceKeyword);
        Assert.NotNull(language.TrustedKeyword);
        Assert.NotNull(language.ProceduralKeyword);
        Assert.Equal(ThreeItems, language.Handlers.Count);
        Assert.Equal("inline plsample_inline", Text(Sql, language.Handlers[1].Span));
        Assert.Empty(ParseWhole<PgCreateLanguageStatement>("create language plpgsql").Handlers);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_drop_and_alter_language()
    {
        const string Sql = "drop procedural language if exists plsample cascade";
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>(Sql).ObjectKind.Count);
        Assert.IsType<PgRenameAction>(Action<PgRenameAction>("alter language x rename to y"));
        SqlAssert.ParseFails(Sql);
    }

    // ---- Foreign data ----

    [Fact]
    public void Parses_create_foreign_data_wrapper()
    {
        const string Sql = "create foreign data wrapper w handler h validator v options (debug 'true')";
        var wrapper = ParseWhole<PgCreateForeignDataWrapperStatement>(Sql);
        Assert.Equal(TwoItems, wrapper.Handlers.Count);
        var option = Assert.Single(wrapper.Options!.Items);
        Assert.Equal("debug 'true'", Text(Sql, option.Span));
        Assert.Equal("options (debug 'true')", Text(Sql, wrapper.Options.Span));
        var none = ParseWhole<PgCreateForeignDataWrapperStatement>("create foreign data wrapper w no handler no validator");
        Assert.All(none.Handlers, handler => Assert.NotNull(handler.NoKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_server()
    {
        const string Sql = "create server if not exists s type 'oracle' version '19' foreign data wrapper oracle_fdw "
            + "options (host 'h', dbname 'db', port '5432')";
        var server = ParseWhole<PgCreateServerStatement>(Sql);
        Assert.NotNull(server.ExistsKeyword);
        Assert.Equal("'oracle'", Text(Sql, server.Type));
        Assert.Equal("'19'", Text(Sql, server.Version));
        Assert.Equal("oracle_fdw", Text(Sql, server.Wrapper));
        Assert.Equal(ThreeItems, server.Options!.Items.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_user_mapping_statements()
    {
        const string Sql = "create user mapping if not exists for current_user server s options (user 'bob', password 'x')";
        var mapping = ParseWhole<PgUserMappingStatement>(Sql);
        Assert.Equal(SyntaxKind.CurrentUserKeyword, mapping.User.Kind);
        Assert.Equal("user 'bob'", Text(Sql, mapping.Options!.Items[0].Span));
        Assert.Equal(SyntaxKind.Identifier, ParseWhole<PgUserMappingStatement>("create user mapping for public server s").User.Kind);
        const string Alter = "alter user mapping for bob server s options (set password 'y')";
        Assert.Equal("set", Text(Alter, ParseWhole<PgUserMappingStatement>(Alter).Options!.Items[0].Action));
        Assert.NotNull(ParseWhole<PgUserMappingStatement>("drop user mapping if exists for bob server s").ExistsKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_foreign_table()
    {
        const string Sql = "create foreign table if not exists ft (id int options (column_name 'id') not null, "
            + "name text collate \"C\" default 'x') inherits (parent) server s options (schema_name 'public')";
        var table = ParseWhole<PgCreateForeignTableStatement>(Sql);
        Assert.Equal(TwoItems, table.Columns.Count);
        Assert.Equal("id int options (column_name 'id') not null", Text(Sql, table.Columns[0].Span));
        Assert.NotNull(table.Columns[0].NotKeyword);
        Assert.NotNull(table.Columns[1].Default);
        Assert.Single(table.Inherits);
        Assert.Equal(NameS, Text(Sql, table.Server));
        Assert.NotNull(table.Options);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create foreign table p1 partition of m for values from ('2020-01-01') to ('2021-01-01') server s", "from")]
    [InlineData("create foreign table p2 partition of m for values in (1, 2) server s", "in")]
    [InlineData("create foreign table p3 partition of m for values with (modulus 4, remainder 0) server s", "with")]
    [InlineData("create foreign table p4 partition of m default server s", "")]
    public void Parses_foreign_table_partitions(string sql, string boundKind)
    {
        var partition = Assert.IsType<PgForeignTablePartition>(ParseWhole<PgCreateForeignTableStatement>(sql).Partition);
        Assert.Equal(boundKind, Text(sql, partition.BoundKind));
        Assert.StartsWith("partition of m", Text(sql, partition.Span), StringComparison.Ordinal);
    }

    [Fact]
    public void Parses_import_foreign_schema()
    {
        const string Sql = "import foreign schema remote limit to (a, b) from server s into local options (import_default 'true')";
        var import = ParseWhole<PgImportForeignSchemaStatement>(Sql);
        Assert.Equal(SyntaxKind.LimitKeyword, import.FilterKeyword?.Kind);
        Assert.Equal(TwoItems, import.Tables.Count);
        Assert.Equal("local", Text(Sql, import.LocalSchema));
        const string Except = "import foreign schema remote except (c) from server s into local";
        Assert.Null(ParseWhole<PgImportForeignSchemaStatement>(Except).ToKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_and_drop_foreign_objects()
    {
        const string Sql = "alter server s version '2' options (add host 'h', set port '1', drop user)";
        var alter = ParseWhole<PgAlterStatement>(Sql);
        Assert.Equal(TwoItems, alter.Actions.Count);
        var options = Assert.IsType<PgOptionsAction>(alter.Actions[1]).Options;
        Assert.Equal("drop user", Text(Sql, options.Items[Third].Span));
        Assert.Null(options.Items[Third].Value);
        const string Wrapper = "alter foreign data wrapper w no handler validator v options (set debug 'false')";
        Assert.Equal(ThreeItems, ParseWhole<PgAlterStatement>(Wrapper).Actions.Count);
        Assert.IsType<PgOptionsAction>(Action<PgOptionsAction>("alter foreign table ft options (drop schema_name)"));
        Assert.NotNull(Action<PgRenameAction>("alter foreign table if exists ft rename column a to b").Kind);
        Assert.Equal(ThreeItems, ParseWhole<PgDropStatement>("drop foreign data wrapper w restrict").ObjectKind.Count);
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>("drop server if exists s1, s2 cascade").Objects.Count);
        Assert.NotNull(ParseWhole<PgDropStatement>("drop foreign table if exists ft").IfKeyword);
        SqlAssert.ParseFails(Sql);
    }

    // ---- Operators, aggregates, casts, collations ----

    [Fact]
    public void Parses_create_operator()
    {
        const string Sql = "create operator === (leftarg = box, rightarg = box, function = area_eq, commutator = ===, "
            + "negator = !==, restrict = area_sel, join = area_join, hashes, merges)";
        var create = ParseWhole<PgCreateOperatorStatement>(Sql);
        Assert.Equal("===", Text(Sql, create.Name.Operator));
        var elements = create.Definition.Elements;
        Assert.Equal(9, elements.Count);
        Assert.Equal("!==", Text(Sql, elements[Fifth].Operator!.Operator));
        Assert.Equal(SyntaxKind.JoinKeyword, elements[Seventh].Name.Kind);
        Assert.Null(elements[^1].EqualsToken);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_prefix_and_qualified_operators()
    {
        const string Sql = "create operator myschema.@@ (rightarg = int, procedure = f)";
        var prefix = ParseWhole<PgCreateOperatorStatement>(Sql);
        Assert.Equal("myschema", Text(Sql, Assert.Single(prefix.Name.SchemaParts)));
        Assert.Equal("myschema.@@", Text(Sql, prefix.Name.Span));
        const string Qualified = "create operator + (leftarg = a, rightarg = a, function = f, commutator = OPERATOR(pg_catalog.+))";
        var commutator = ParseWhole<PgCreateOperatorStatement>(Qualified).Definition.Elements[^1].Operator!;
        Assert.Equal("OPERATOR(pg_catalog.+)", Text(Qualified, commutator.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_drop_and_alter_operator()
    {
        const string Sql = "drop operator if exists ===(box, box), @@ (none, int) cascade";
        var drop = ParseWhole<PgDropStatement>(Sql);
        Assert.Equal(TwoItems, drop.Objects.Count);
        Assert.Equal("@@ (none, int)", Text(Sql, drop.Objects[1].Span));
        Assert.IsType<PgOwnerAction>(Action<PgOwnerAction>("alter operator === (box, box) owner to bob"));
        const string Set = "alter operator @@ (none, int) set (restrict = r, join = j)";
        Assert.Equal(TwoItems, Action<PgDefinitionAction>(Set).Definition.Elements.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_operator_class()
    {
        const string Sql = "create operator class gist__int_ops default for type _int4 using gist family f as "
            + "operator 3 &&, operator 6 = (anyarray, anyarray), operator 15 <-> (int, int) for order by float_ops, "
            + "function 1 g_int_consistent (internal, _int4, smallint, oid, internal), function 2 (int, int) g_union(internal), storage _int4";
        var opclass = ParseWhole<PgCreateOperatorClassStatement>(Sql);
        Assert.NotNull(opclass.DefaultKeyword);
        Assert.Equal("_int4", Text(Sql, opclass.Type!.Span));
        Assert.Equal("gist", Text(Sql, opclass.AccessMethod));
        Assert.Equal(SixItems, opclass.Items.Count);
        Assert.Equal("operator 15 <-> (int, int) for order by float_ops", Text(Sql, opclass.Items[Third].Span));
        Assert.Equal(FiveItems, opclass.Items[Fourth].Function!.Arguments.Count);
        Assert.Equal(TwoItems, opclass.Items[Fifth].Types.Count);
        Assert.NotNull(opclass.Items[^1].StorageType);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_operator_family_statements()
    {
        const string Sql = "create operator family f using btree";
        Assert.Empty(ParseWhole<PgCreateOperatorClassStatement>(Sql).Items);
        const string Add = "alter operator family f using btree add operator 1 < (int, int) for search, function 1 (int, int) btint4cmp(int, int)";
        var add = Action<PgOperatorFamilyAction>(Add);
        Assert.Equal(SyntaxKind.SearchKeyword, add.Items[0].Purpose?.Kind);
        Assert.NotNull(add.Items[1].Function);
        const string Drop = "alter operator family f using btree drop operator 1 (int, int), function 1 (int, int)";
        Assert.All(Action<PgOperatorFamilyAction>(Drop).Items, item => Assert.Null(item.Operator));
        Assert.Equal("btree", Text("drop operator class if exists c using btree cascade",
            ParseWhole<PgDropStatement>("drop operator class if exists c using btree cascade").Objects[0].AccessMethod));
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>("drop operator family f using gist").ObjectKind.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_aggregate()
    {
        const string Sql = "create or replace aggregate avg2 (float8) (sfunc = float8_accum, stype = float8[], "
            + "finalfunc = float8_avg, initcond = '{0,0,0}', parallel = safe)";
        var aggregate = ParseWhole<PgCreateAggregateStatement>(Sql);
        Assert.NotNull(aggregate.ReplaceKeyword);
        Assert.Equal("avg2 (float8)", Text(Sql, aggregate.Signature.Span));
        Assert.Single(aggregate.Signature.Arguments);
        Assert.Equal("float8[]", Text(Sql, aggregate.Definition.Elements[1].Type!.Span));
        Assert.Equal("'{0,0,0}'", Text(Sql, aggregate.Definition.Elements[Fourth].Literal));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_ordered_set_star_and_old_style_aggregates()
    {
        const string Ordered = "create aggregate pct (float8 order by anyelement) (sfunc = s, stype = internal, finalfunc_extra)";
        var ordered = ParseWhole<PgCreateAggregateStatement>(Ordered).Signature;
        Assert.NotNull(ordered.OrderKeyword);
        Assert.Single(ordered.OrderedArguments);
        Assert.NotNull(ParseWhole<PgCreateAggregateStatement>("create aggregate cnt (*) (sfunc = int8inc, stype = int8)").Signature.Star);
        const string Old = "create aggregate old_sum (basetype = int4, sfunc = int4pl, stype = int4)";
        var old = ParseWhole<PgCreateAggregateStatement>(Old);
        Assert.Null(old.Signature.OpenParen);
        Assert.Equal("basetype", Text(Old, old.Definition.Elements[0].Name));
        SqlAssert.ParseFails(Old);
    }

    [Fact]
    public void Parses_drop_and_alter_aggregate()
    {
        const string Sql = "drop aggregate if exists avg2(float8), cnt(*)";
        var drop = ParseWhole<PgDropStatement>(Sql);
        Assert.Equal("avg2(float8)", Text(Sql, drop.Objects[0].Span));
        Assert.NotNull(drop.Objects[1].Star);
        Assert.IsType<PgRenameAction>(Action<PgRenameAction>("alter aggregate avg2(float8) rename to avg3"));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_cast_postgres_forms()
    {
        const string Sql = "create cast (bigint as int4) with function int4(bigint) as assignment";
        var cast = ParseWhole<CreateCastStatement>(Sql);
        Assert.Equal("int4", Text(Sql, cast.Routine!.Name[0]));
        Assert.Single(cast.Postgres!.Arguments);
        Assert.Equal("(", Text(Sql, cast.Postgres!.ArgumentsOpenParen));
        Assert.Equal(")", Text(Sql, cast.Postgres!.ArgumentsCloseParen));
        SqlAssert.ParseFails(Sql);

        const string Without = "create cast (text as mytype) without function as implicit";
        var without = ParseWhole<CreateCastStatement>(Without);
        Assert.Null(without.Routine);
        Assert.NotNull(without.Postgres!.FunctionKeyword);
        Assert.Equal("implicit", Text(Without, without.AssignmentKeyword));
        SqlAssert.ParseFails(Without);

        const string Inout = "create cast (mytype as text) with inout";
        Assert.NotNull(ParseWhole<CreateCastStatement>(Inout).Postgres!.InoutKeyword);
        SqlAssert.ParseFails(Inout);
        Assert.NotNull(SqlAssert.ParsePg<CreateCastStatement>("create cast (integer as emp_id) with function int_to_emp as assignment").Routine);
    }

    [Fact]
    public void Parses_drop_cast()
    {
        const string Sql = "drop cast if exists (bigint as int4) cascade";
        var drop = ParseWhole<PgDropStatement>(Sql);
        var reference = Assert.Single(drop.Objects);
        Assert.Equal("(bigint as int4)", Text(Sql, reference.Span));
        Assert.Equal(TwoItems, reference.Arguments.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_collation_postgres_forms()
    {
        const string Sql = "create collation if not exists german (provider = icu, locale = 'de-DE', deterministic = false)";
        var collation = ParseWhole<PgCreateCollationStatement>(Sql);
        Assert.NotNull(collation.ExistsKeyword);
        var elements = collation.Definition!.Elements;
        Assert.Equal("icu", Text(Sql, elements[0].Type!.Span));
        Assert.Equal(SyntaxKind.FalseKeyword, elements[Third].Literal?.Kind);
        SqlAssert.ParseFails(Sql);
        const string From = "create collation german2 from \"de_DE\"";
        Assert.NotNull(ParseWhole<PgCreateCollationStatement>(From).Source);
        Assert.IsType<CreateCollationStatement>(SqlAssert.ParsePg<CreateCollationStatement>("create collation sch.en for utf8 from ucs_basic no pad"));
        Assert.IsType<PgKeywordAction>(Action<PgKeywordAction>("alter collation c refresh version"));
        Assert.NotNull(ParseWhole<PgDropStatement>("drop collation if exists german cascade").IfKeyword);
    }

    [Fact]
    public void Parses_conversion_statistics_and_access_method()
    {
        const string Sql = "create default conversion myconv for 'UTF8' to 'LATIN1' from myfunc";
        var conversion = ParseWhole<PgCreateConversionStatement>(Sql);
        Assert.NotNull(conversion.DefaultKeyword);
        Assert.Equal("'LATIN1'", Text(Sql, conversion.TargetEncoding));
        SqlAssert.ParseFails(Sql);
        const string Statistics = "create statistics if not exists s1 (ndistinct, dependencies) on a, (b + c) from t";
        var statistics = ParseWhole<PgCreateStatisticsStatement>(Statistics);
        Assert.Equal(TwoItems, statistics.Kinds.Count);
        Assert.Equal(TwoItems, statistics.Expressions.Count);
        Assert.Null(ParseWhole<PgCreateStatisticsStatement>("create statistics on a, b from t").Name);
        const string Method = "create access method heap2 type table handler heap_tableam_handler";
        Assert.Equal("table", Text(Method, ParseWhole<PgCreateAccessMethodStatement>(Method).Kind));
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>("drop access method heap2").ObjectKind.Count);
        Assert.NotNull(ParseWhole<PgDropStatement>("drop statistics if exists s1").IfKeyword);
        Assert.Single(ParseWhole<PgDropStatement>("drop conversion myconv").Objects);
    }

    // ---- Full text search ----

    [Theory]
    [InlineData("create text search configuration my_cfg (parser = default)", "configuration", OneItem)]
    [InlineData("create text search configuration my_cfg2 (copy = english)", "configuration", OneItem)]
    [InlineData("create text search dictionary d (template = snowball, language = russian, stopwords = myrussian)", "dictionary", ThreeItems)]
    [InlineData("create text search parser p (start = s, gettoken = g, end = e, lextypes = l, headline = h)", "parser", FiveItems)]
    [InlineData("create text search template tpl (init = dsimple_init, lexize = dsimple_lexize)", "template", TwoItems)]
    public void Parses_create_text_search(string sql, string kind, int elementCount)
    {
        var create = ParseWhole<PgCreateTextSearchStatement>(sql);
        Assert.Equal(kind, Text(sql, create.KindKeyword));
        Assert.Equal(elementCount, create.Definition.Elements.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_text_search_configuration_mappings()
    {
        const string Sql = "alter text search configuration my_cfg add mapping for word, asciiword with my_dict, english_stem";
        var add = Action<PgTextSearchMappingAction>(Sql);
        Assert.Equal(TwoItems, add.TokenTypes.Count);
        Assert.Equal(TwoItems, add.Dictionaries.Count);
        Assert.Equal("add mapping for word, asciiword with my_dict, english_stem", Text(Sql, add.Span));
        const string Replace = "alter text search configuration my_cfg alter mapping for word replace english_stem with my_stem";
        Assert.NotNull(Action<PgTextSearchMappingAction>(Replace).ReplaceKeyword);
        Assert.Empty(Action<PgTextSearchMappingAction>("alter text search configuration my_cfg alter mapping replace a with b").TokenTypes);
        Assert.NotNull(Action<PgTextSearchMappingAction>("alter text search configuration my_cfg drop mapping if exists for word").ExistsKeyword);
        Assert.IsType<PgDefinitionAction>(Action<PgDefinitionAction>("alter text search dictionary d (stopwords = newrussian)"));
        Assert.Equal(ThreeItems, ParseWhole<PgDropStatement>("drop text search parser if exists p cascade").ObjectKind.Count);
        SqlAssert.ParseFails(Sql);
    }

    // ---- Replication ----

    [Fact]
    public void Parses_create_publication()
    {
        const string Sql = "create publication pub for table only t1 (a, b) where (a > 1), t2, tables in schema s1, current_schema "
            + "with (publish = 'insert,update')";
        var publication = ParseWhole<PgCreatePublicationStatement>(Sql);
        Assert.Equal(FourItems, publication.Objects.Count);
        var first = publication.Objects[0];
        Assert.Equal("table only t1 (a, b) where (a > 1)", Text(Sql, first.Span));
        Assert.NotNull(first.Where);
        Assert.Null(publication.Objects[1].TableKeyword);
        Assert.NotNull(publication.Objects[Third].SchemaKeyword);
        Assert.Equal(SyntaxKind.CurrentSchemaKeyword, publication.Objects[Fourth].Name[0].Kind);
        Assert.Single(publication.Definition!.Elements);
        Assert.NotNull(ParseWhole<PgCreatePublicationStatement>("create publication allpub for all tables").AllKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_and_drop_publication()
    {
        const string Sql = "alter publication pub add table t3 where (x = 1)";
        Assert.NotNull(Assert.Single(Action<PgPublicationObjectsAction>(Sql).Objects).Where);
        Assert.NotNull(Action<PgPublicationObjectsAction>("alter publication pub set tables in schema s").Objects[0].InKeyword);
        Assert.Equal(TwoItems, Action<PgPublicationObjectsAction>("alter publication pub drop table t1, t2").Objects.Count);
        Assert.IsType<PgDefinitionAction>(Action<PgDefinitionAction>("alter publication pub set (publish = 'insert')"));
        Assert.IsType<PgOwnerAction>(Action<PgOwnerAction>("alter publication pub owner to bob"));
        Assert.IsType<PgRenameAction>(Action<PgRenameAction>("alter publication pub rename to pub2"));
        Assert.NotNull(ParseWhole<PgDropStatement>("drop publication if exists pub cascade").Behavior);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_subscription()
    {
        const string Sql = "create subscription sub connection 'host=h dbname=d' publication p1, p2 with (copy_data = false, slot_name = none)";
        var subscription = ParseWhole<PgCreateSubscriptionStatement>(Sql);
        Assert.Equal("'host=h dbname=d'", Text(Sql, subscription.Connection));
        Assert.Equal(TwoItems, subscription.Publications.Count);
        Assert.Equal(TwoItems, subscription.Definition!.Elements.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter subscription sub connection 'host=x'", OneItem)]
    [InlineData("alter subscription sub set publication p with (refresh = false)", TwoItems)]
    [InlineData("alter subscription sub add publication p3", TwoItems)]
    [InlineData("alter subscription sub drop publication p3", TwoItems)]
    [InlineData("alter subscription sub refresh publication with (copy_data = false)", TwoItems)]
    [InlineData("alter subscription sub enable", OneItem)]
    [InlineData("alter subscription sub disable", OneItem)]
    public void Parses_alter_subscription_commands(string sql, int keywordCount)
    {
        var action = Action<PgKeywordAction>(sql);
        Assert.Equal(keywordCount, action.Keywords.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_subscription_definitions_and_drop()
    {
        const string Sql = "alter subscription sub skip (lsn = '0/12345')";
        Assert.Equal("skip", Text(Sql, Action<PgDefinitionAction>(Sql).Keyword));
        Assert.IsType<PgDefinitionAction>(Action<PgDefinitionAction>("alter subscription sub set (slot_name = none)"));
        Assert.IsType<PgOwnerAction>(Action<PgOwnerAction>("alter subscription sub owner to bob"));
        Assert.IsType<PgRenameAction>(Action<PgRenameAction>("alter subscription sub rename to sub2"));
        Assert.NotNull(ParseWhole<PgDropStatement>("drop subscription if exists sub").IfKeyword);
        SqlAssert.ParseFails(Sql);
    }

    // ---- Triggers and event triggers ----

    [Fact]
    public void Parses_trigger_for_each_row_with_execute_function()
    {
        const string Sql = "create trigger tr after insert on t for each row execute function f()";
        var trigger = ParseWhole<CreateTriggerStatement>(Sql);
        Assert.Equal("row", Text(Sql, trigger.Granularity));
        var body = Assert.IsType<PgExecuteFunctionClause>(trigger.Body);
        Assert.Equal("execute function f()", Text(Sql, body.Span));
        Assert.Empty(body.Arguments);
        SqlAssert.ParseFails(Sql);
        const string Statement = "create trigger tr after insert on t for statement execute function f()";
        Assert.Null(ParseWhole<CreateTriggerStatement>(Statement).EachKeyword);
    }

    [Fact]
    public void Parses_trigger_when_condition()
    {
        const string Sql = "create trigger tr before update on t for each row when (old.a is distinct from new.a) execute function f()";
        var trigger = ParseWhole<CreateTriggerStatement>(Sql);
        Assert.Equal("old.a is distinct from new.a", Text(Sql, trigger.When!.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_trigger_transition_tables()
    {
        const string Sql = "create trigger tr after update on t referencing old table as o new table as n for each statement execute function f()";
        var trigger = ParseWhole<CreateTriggerStatement>(Sql);
        Assert.Equal(TwoItems, trigger.Transitions.Count);
        Assert.Equal("new table as n", Text(Sql, trigger.Transitions[1].Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_constraint_trigger()
    {
        const string Sql = "create constraint trigger ct after insert on t from ref not deferrable for each row execute procedure f('a', 1)";
        var trigger = ParseWhole<CreateTriggerStatement>(Sql);
        Assert.NotNull(trigger.Postgres!.ConstraintKeyword);
        Assert.Equal("ref", Text(Sql, Assert.Single(trigger.Postgres!.ReferencedTable!)));
        Assert.NotNull(trigger.Postgres!.DeferrableNotKeyword);
        Assert.Equal(TwoItems, Assert.IsType<PgExecuteFunctionClause>(trigger.Body).Arguments.Count);
        const string Deferred = "create constraint trigger ct after insert on t deferrable initially deferred for each row execute procedure f()";
        Assert.Equal("deferred", Text(Deferred, ParseWhole<CreateTriggerStatement>(Deferred).Postgres!.InitiallyWhen));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_execute_procedure_and_or_replace_events()
    {
        const string Sql = "create or replace trigger tr before insert or update of a, b or delete or truncate on t execute procedure f()";
        var trigger = ParseWhole<CreateTriggerStatement>(Sql);
        Assert.NotNull(trigger.Postgres!.ReplaceKeyword);
        Assert.Equal(ThreeItems, trigger.Postgres!.AdditionalEvents.Count);
        Assert.Equal("or update of a, b", Text(Sql, trigger.Postgres!.AdditionalEvents[0].Span));
        Assert.Equal("truncate", Text(Sql, trigger.Postgres!.AdditionalEvents[^1].Event));
        Assert.Equal("procedure", Text(Sql, Assert.IsType<PgExecuteFunctionClause>(trigger.Body).FunctionKeyword));
        Assert.NotNull(ParseWhole<CreateTriggerStatement>("create trigger tr instead of insert on v for each row execute function f()").OfKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_event_trigger()
    {
        const string Sql = "create event trigger et on ddl_command_start when tag in ('CREATE TABLE', 'DROP TABLE') and tag in ('x') "
            + "execute function f()";
        var trigger = ParseWhole<PgCreateEventTriggerStatement>(Sql);
        Assert.Equal("ddl_command_start", Text(Sql, trigger.Event));
        Assert.Equal(TwoItems, trigger.Filters.Count);
        Assert.Equal(TwoItems, trigger.Filters[0].Values.Count);
        Assert.Equal("and tag in ('x')", Text(Sql, trigger.Filters[1].Span));
        Assert.Empty(ParseWhole<PgCreateEventTriggerStatement>("create event trigger et2 on sql_drop execute procedure f()").Filters);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("alter event trigger et disable", OneItem)]
    [InlineData("alter event trigger et enable", OneItem)]
    [InlineData("alter event trigger et enable replica", TwoItems)]
    [InlineData("alter event trigger et enable always", TwoItems)]
    public void Parses_alter_event_trigger_enablement(string sql, int keywordCount)
    {
        Assert.Equal(keywordCount, Action<PgKeywordAction>(sql).Keywords.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_and_drop_event_trigger()
    {
        Assert.IsType<PgOwnerAction>(Action<PgOwnerAction>("alter event trigger et owner to bob"));
        Assert.IsType<PgRenameAction>(Action<PgRenameAction>("alter event trigger et rename to et3"));
        const string Sql = "drop event trigger if exists et cascade";
        Assert.Equal(TwoItems, ParseWhole<PgDropStatement>(Sql).ObjectKind.Count);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_alter_and_drop_trigger()
    {
        const string Sql = "alter trigger tr on t rename to tr2";
        var alter = ParseWhole<PgAlterStatement>(Sql);
        Assert.Equal(NameT, Text(Sql, Assert.Single(alter.Table!)));
        Assert.IsType<PgRenameAction>(Assert.Single(alter.Actions));
        Assert.Equal(ThreeItems, Action<PgKeywordAction>("alter trigger tr on t depends on extension e").Keywords.Count);
        SqlAssert.ParseFails(Sql);

        const string Drop = "drop trigger if exists tr on t cascade";
        var drop = ParseWhole<DropTriggerStatement>(Drop);
        Assert.NotNull(drop.Postgres!.ExistsKeyword);
        Assert.Equal(NameT, Text(Drop, Assert.Single(drop.Postgres!.TableName!)));
        Assert.Equal(Cascade, Text(Drop, drop.Postgres!.Behavior));
        SqlAssert.ParseFails(Drop);
        Assert.Null(SqlAssert.ParsePg<DropTriggerStatement>("drop trigger sch.tr").Postgres?.OnKeyword);
    }

    [Fact]
    public void Schema_hook_leaves_other_statements_alone()
    {
        Assert.IsType<CreateTableStatement>(SqlAssert.ParsePg<CreateTableStatement>("create table t (a int)"));
        Assert.IsType<AlterDomainStatement>(SqlAssert.ParsePg<AlterDomainStatement>("alter domain d set default 1"));
        Assert.IsType<DropTableStatement>(SqlAssert.ParsePg<DropTableStatement>("drop table t cascade"));
        Assert.IsType<DirectSqlScript>(SqlAssert.ParsePg<DirectSqlScript>("create type e as enum ('a'); select 1"));
    }
}
