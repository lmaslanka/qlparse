namespace QlParse.Tests;

public sealed class ParseDdlTests
{
    private sealed class EmptyVisitor : SqlVisitor;

    private const string BareResignal = "resignal;";

    [Fact]
    public void Parses_create_schema()
    {
        var named = SqlAssert.Parse<SchemaDefinition>("create schema cat.s authorization u");
        Assert.Equal(Count.Two, named.Name!.Count);
        Assert.NotNull(named.Authorization);

        var authOnly = SqlAssert.Parse<SchemaDefinition>("create schema authorization u");
        Assert.Null(authOnly.Name);
        Assert.NotNull(authOnly.AuthorizationKeyword);
    }

    [Fact]
    public void Parses_schema_path_and_default_character_set()
    {
        var schema = SqlAssert.Parse<SchemaDefinition>("create schema s default character set latin1 path a, b.s");
        Assert.NotNull(schema.DefaultKeyword);
        Assert.NotNull(schema.SetKeyword);
        Assert.Equal(Count.Two, schema.Path!.Count);
        Assert.Equal(Count.Two, schema.Path[1].Count);

        var reversed = SqlAssert.Parse<SchemaDefinition>("create schema s path p default character set utf8");
        Assert.NotNull(reversed.PathKeyword);
        Assert.NotNull(reversed.CharacterSet);
    }

    [Fact]
    public void Parses_schema_with_table()
    {
        var schema = SqlAssert.Parse<SchemaDefinition>("create schema s create table t (a integer)");
        Assert.Single(schema.Elements);
        Assert.IsType<ColumnDefinition>(schema.Elements[0].Elements![0]);
        new EmptyVisitor().Visit(schema);
    }

    [Fact]
    public void Parses_alter_and_drop_schema()
    {
        var altered = SqlAssert.Parse<AlterSchemaStatement>("alter schema cat.s rename to t");
        Assert.Equal(Count.Two, altered.Name.Count);
        Assert.Single(altered.NewName);

        Assert.IsType<DropSchemaStatement>(SqlAssert.Parse<DropSchemaStatement>("drop schema s cascade"));
        Assert.IsType<DropSchemaStatement>(Sql.Parse("drop schema cat.s restrict").Root);
    }

    [Fact]
    public void Parses_create_table()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("create table s.t (a integer, b character varying(10));");
        Assert.Equal(Count.Two, table.Name.Count);
        Assert.Equal(Count.Two, table.Elements!.Count);
        Assert.IsType<ColumnDefinition>(table.Elements[1]);
    }

    [Fact]
    public void Parses_create_table_as()
    {
        var columns = SqlAssert.Parse<CreateTableStatement>("create table t (a, b) as select 1, 2 with data");
        Assert.Equal(Count.Two, columns.AsColumns!.Count);
        Assert.IsType<SelectStatement>(columns.Query);
        Assert.Null(columns.NoKeyword);

        var none = SqlAssert.Parse<CreateTableStatement>("create table t as select 1 with no data");
        Assert.NotNull(none.NoKeyword);
        Assert.NotNull(none.DataKeyword);
    }

    [Fact]
    public void Parses_create_table_of()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("create table emp of sch.person under people (ref is id system generated, name with options default 'x')");
        Assert.NotNull(table.OfKeyword);
        Assert.Equal(Count.Two, table.TypeName!.Count);
        Assert.NotNull(table.UnderKeyword);
        Assert.IsType<RefIsClause>(table.Elements![0]);
        var column = Assert.IsType<ColumnDefinition>(table.Elements[1]);
        Assert.NotNull(column.OptionsKeyword);
        Assert.NotNull(column.Default);

        var user = SqlAssert.Parse<CreateTableStatement>("create table emp of person (ref is id user generated)");
        Assert.Equal(SyntaxKind.UserKeyword, Assert.IsType<RefIsClause>(user.Elements![0]).Generation!.Value.Kind);
        var derived = SqlAssert.Parse<CreateTableStatement>("create table managers of manager under emp");
        Assert.NotNull(derived.UnderKeyword);
        Assert.Null(derived.Elements);

        var scoped = SqlAssert.Parse<CreateTableStatement>("create table emp of person (name with options scope sch.people)");
        var options = Assert.IsType<ColumnDefinition>(scoped.Elements![0]);
        Assert.NotNull(options.ScopeKeyword);
        Assert.Equal(Count.Two, options.ScopeName!.Count);
        new EmptyVisitor().Visit(scoped);
    }

    [Fact]
    public void Parses_temporary_tables()
    {
        var global = SqlAssert.Parse<CreateTableStatement>("create global temporary table t (a integer) on commit preserve rows");
        Assert.NotNull(global.Scope);
        Assert.NotNull(global.TemporaryKeyword);
        Assert.NotNull(global.CommitAction);
        Assert.NotNull(global.RowsKeyword);

        var local = SqlAssert.Parse<CreateTableStatement>("create local temporary table t (a integer) on commit delete rows");
        Assert.NotNull(local.OnKeyword);
        Assert.NotNull(local.CommitKeyword);
    }

    [Fact]
    public void Parses_generated_and_identity_columns()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("""
            create table t (
              a integer generated always as identity (start with 1 increment by 1 no cycle),
              b integer generated by default as identity,
              c integer generated always as (a + 1),
              d timestamp generated always as row start,
              e timestamp generated always as row end
            )
            """);
        var identity = Assert.IsType<ColumnDefinition>(table.Elements![0]).Identity;
        Assert.NotNull(identity);
        Assert.Equal(Count.Three, identity.Options.Count);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(table.Elements[1]).Identity!.DefaultKeyword);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(table.Elements[Count.Two]).Generated!.Expression);
        Assert.Equal(SyntaxKind.EndKeyword, Assert.IsType<ColumnDefinition>(table.Elements[Count.Four]).Generated!.Bound!.Value.Kind);

        var options = SqlAssert.Parse<CreateTableStatement>("""
            create table t (
              a integer generated always as identity (
                start with 1 increment by 2 minvalue 1 maxvalue 9 cycle no maxvalue no minvalue no cycle
              )
            )
            """);
        Assert.Equal(Count.Eight, Assert.IsType<ColumnDefinition>(options.Elements![0]).Identity!.Options.Count);
        var increment = Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set increment by 2").Action);
        Assert.NotNull(increment.IdentityOption);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter column a set no cycle").Action).IdentityOption);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set maxvalue 10").Action).IdentityOption!.Value);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set minvalue 1").Action).IdentityOption);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set cycle").Action).IdentityOption);
        new EmptyVisitor().Visit(table);
        new EmptyVisitor().Visit(increment);
    }

    [Fact]
    public void Parses_temporal_tables()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("""
            create table emp (
              id integer,
              valid_start date,
              valid_end date,
              sys_start timestamp generated always as row start,
              sys_end timestamp generated always as row end,
              period for dept_period (valid_start, valid_end),
              period for system_time (sys_start, sys_end)
            ) with system versioning
            """);
        Assert.Equal(Count.Two, table.Elements!.OfType<PeriodDefinition>().Count());
        Assert.NotNull(table.VersioningKeyword);
        Assert.NotNull(table.WithKeyword);

        var added = Assert.IsType<AddPeriodAction>(SqlAssert.Parse<AlterTableStatement>("alter table emp add period for dept_period (valid_start, valid_end)").Action);
        Assert.Equal(SyntaxKind.Identifier, added.Period.EndColumn.Kind);
        Assert.IsType<DropPeriodAction>(SqlAssert.Parse<AlterTableStatement>("alter table emp drop period for dept_period cascade").Action);
        Assert.IsType<SystemVersioningAction>(SqlAssert.Parse<AlterTableStatement>("alter table emp add system versioning").Action);
        Assert.IsType<SystemVersioningAction>(SqlAssert.Parse<AlterTableStatement>("alter table emp drop system versioning").Action);
        new EmptyVisitor().Visit(table);
        new EmptyVisitor().Visit(added);
    }

    [Fact]
    public void Parses_property_graph()
    {
        var graph = SqlAssert.Parse<CreatePropertyGraphStatement>("""
            create property graph sch.g
              vertex tables ( person key ( id ) label person properties ( name ) )
              edge tables (
                knows source key ( src ) references person ( id )
                  destination key ( dst ) references person ( id )
                  label knows properties all
              )
            """);
        Assert.Equal(Count.Two, graph.Name.Count);
        Assert.NotNull(graph.Vertices[0].KeyKeyword);
        Assert.NotNull(graph.Edges[0].DestinationKeyword);
        Assert.IsType<DropPropertyGraphStatement>(SqlAssert.Parse<DropPropertyGraphStatement>("drop property graph g cascade"));
        new EmptyVisitor().Visit(graph);
    }

    [Fact]
    public void Parses_like_table()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("create table t (like u including defaults excluding identity including generated, a integer)");
        var like = Assert.IsType<LikeClause>(table.Elements![0]);
        Assert.Equal(Count.Three, like.Options.Count);
        Assert.IsType<ColumnDefinition>(table.Elements[1]);
    }

    [Fact]
    public void Parses_alter_table()
    {
        Assert.IsType<AddColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t add column a integer").Action);
        var scope = Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a add scope people").Action);
        Assert.NotNull(scope.ScopeName);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a drop scope cascade").Action).ScopeKeyword);
        Assert.IsType<DropColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t drop column a cascade").Action);
        var setDefault = Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter column a set default 1").Action);
        Assert.NotNull(setDefault.Default);
        var dataType = Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set data type character varying(10)").Action);
        Assert.NotNull(dataType.DataType);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a drop identity").Action).IdentityKeyword);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(SqlAssert.Parse<AlterTableStatement>("alter table t alter a restart with 5").Action).RestartValue);
        Assert.IsType<DropConstraintAction>(SqlAssert.Parse<AlterTableStatement>("alter table t drop constraint c restrict").Action);
        Assert.IsType<AddConstraintAction>(SqlAssert.Parse<AlterTableStatement>("alter table t add primary key (a)").Action);
        new EmptyVisitor().Visit(SqlAssert.Parse<AlterTableStatement>("alter table t alter a set data type integer"));
    }

    [Fact]
    public void Parses_drop_table()
    {
        var dropped = SqlAssert.Parse<DropTableStatement>("drop table s.t cascade");
        Assert.Equal(Count.Two, dropped.Name.Count);
    }

    [Fact]
    public void Parses_table_constraints()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("""
            create table t (
              a integer not null unique,
              b integer references u (b) on delete cascade,
              constraint pk primary key (a),
              constraint fk foreign key (b) references u (b) match full on update set null deferrable initially deferred,
              unique nulls not distinct (a),
              check (a > 0)
            )
            """);
        Assert.Equal(Count.Two, Assert.IsType<ColumnDefinition>(table.Elements![0]).Constraints.Count);
        Assert.Equal(Count.Six, table.Elements.Count);
        new EmptyVisitor().Visit(table);
    }

    [Fact]
    public void Parses_constraint_checklist()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("""
            create table t (
              a integer constraint nn not null default 1,
              b integer unique nulls distinct,
              c integer primary key,
              d integer references u match simple on update cascade on delete set default,
              e integer check (e > 0) not deferrable initially immediate,
              unique nulls not distinct (b),
              constraint pk primary key (a),
              constraint fk foreign key (d) references u (d) match partial on delete no action on update restrict,
              check (a > 0) deferrable initially deferred
            )
            """);
        var namedNull = Assert.IsType<ColumnDefinition>(table.Elements![0]);
        Assert.NotNull(namedNull.Default);
        Assert.NotNull(namedNull.Constraints[0].ConstraintName);
        Assert.NotNull(namedNull.Constraints[0].NullKeyword);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(table.Elements[1]).Constraints[0].DistinctKeyword);
        Assert.Null(Assert.IsType<ColumnDefinition>(table.Elements[1]).Constraints[0].NullsNotKeyword);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(table.Elements[Count.Two]).Constraints[0].PrimaryKeyword);
        var reference = Assert.IsType<ColumnDefinition>(table.Elements[Count.Three]).Constraints[0];
        Assert.Equal(SyntaxKind.MatchKeyword, reference.MatchKeyword!.Value.Kind);
        Assert.Equal(Count.Two, reference.Actions.Count);
        var check = Assert.IsType<ColumnDefinition>(table.Elements[Count.Four]).Constraints[0];
        Assert.NotNull(check.DeferrableNotKeyword);
        Assert.NotNull(check.InitiallyWhen);
        var unique = Assert.IsType<TableConstraint>(table.Elements[Count.Five]);
        Assert.NotNull(unique.NullsNotKeyword);
        var foreign = Assert.IsType<TableConstraint>(table.Elements[Count.Seven]);
        Assert.NotNull(foreign.ForeignKeyword);
        Assert.Equal(SyntaxKind.PartialKeyword, foreign.MatchType!.Value.Kind);
        Assert.NotNull(foreign.Actions[0].NoKeyword);
        var deferred = Assert.IsType<TableConstraint>(table.Elements[Count.Eight]);
        Assert.NotNull(deferred.DeferrableKeyword);
        Assert.NotNull(deferred.InitiallyKeyword);
        new EmptyVisitor().Visit(table);
    }

    [Fact]
    public void Parses_create_view()
    {
        var view = SqlAssert.Parse<CreateViewStatement>("create view s.v (a, b) as select 1, 2");
        Assert.Equal(Count.Two, view.Name.Count);
        Assert.Equal(Count.Two, view.Columns!.Count);
        Assert.IsType<SelectStatement>(view.Query);

        var recursive = SqlAssert.Parse<CreateViewStatement>("create recursive view v (n) as select 1");
        Assert.NotNull(recursive.RecursiveKeyword);
        Assert.Null(recursive.CheckKeyword);

        var typed = SqlAssert.Parse<CreateViewStatement>("create view emp of sch.person under people (ref is id system generated, name with options default 'x') as select 1, 'x'");
        Assert.NotNull(typed.OfKeyword);
        Assert.Equal(Count.Two, typed.TypeName!.Count);
        Assert.NotNull(typed.UnderKeyword);
        Assert.IsType<RefIsClause>(typed.Elements![0]);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(typed.Elements[1]).Default);
        new EmptyVisitor().Visit(typed);
    }

    [Fact]
    public void Parses_view_check_option()
    {
        var plain = SqlAssert.Parse<CreateViewStatement>("create view v as select 1 with check option");
        Assert.NotNull(plain.WithKeyword);
        Assert.Null(plain.Levels);
        Assert.NotNull(plain.OptionKeyword);

        var cascaded = SqlAssert.Parse<CreateViewStatement>("create view v as select 1 with cascaded check option");
        Assert.NotNull(cascaded.Levels);
        Assert.NotNull(cascaded.CheckKeyword);

        var local = SqlAssert.Parse<CreateViewStatement>("create view v as select 1 union select 2 with local check option");
        Assert.NotNull(local.Levels);
        Assert.IsType<SetOperation>(local.Query);
    }

    [Fact]
    public void Parses_alter_and_drop_view()
    {
        var replaced = SqlAssert.Parse<AlterViewStatement>("alter view s.v as select 1");
        Assert.IsType<ReplaceViewAction>(replaced.Action);
        var column = Assert.IsType<AlterViewColumnAction>(SqlAssert.Parse<AlterViewStatement>("alter view v alter column a set data type integer").Action);
        Assert.NotNull(column.Column);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(column.Column).DataType);
        var scope = Assert.IsType<AlterViewColumnAction>(SqlAssert.Parse<AlterViewStatement>("alter view v alter a add scope people").Action);
        Assert.NotNull(Assert.IsType<AlterColumnAction>(scope.Column).ScopeName);
        new EmptyVisitor().Visit(replaced);

        var dropped = SqlAssert.Parse<DropViewStatement>("drop view s.v cascade");
        Assert.Equal(Count.Two, dropped.Name.Count);
    }

    [Fact]
    public void Parses_domain()
    {
        var domain = SqlAssert.Parse<CreateDomainStatement>("create domain sch.d as integer default 1 constraint c check (value > 0) deferrable initially deferred collate en");
        Assert.Equal(Count.Two, domain.Name.Count);
        Assert.NotNull(domain.AsKeyword);
        Assert.NotNull(domain.Default);
        Assert.NotNull(domain.CollateKeyword);
        var constraint = Assert.Single(domain.Constraints);
        Assert.NotNull(constraint.ConstraintName);
        Assert.NotNull(constraint.InitiallyWhen);

        var bare = SqlAssert.Parse<CreateDomainStatement>("create domain d character varying(10)");
        Assert.Null(bare.AsKeyword);
        Assert.Empty(bare.Constraints);

        Assert.IsType<SetDomainDefaultAction>(SqlAssert.Parse<AlterDomainStatement>("alter domain d set default 1").Action);
        Assert.IsType<DropDomainDefaultAction>(SqlAssert.Parse<AlterDomainStatement>("alter domain d drop default").Action);
        Assert.IsType<AddDomainConstraintAction>(SqlAssert.Parse<AlterDomainStatement>("alter domain d add check (value > 0)").Action);
        Assert.IsType<DropDomainConstraintAction>(SqlAssert.Parse<AlterDomainStatement>("alter domain d drop constraint c cascade").Action);
        Assert.IsType<DropDomainStatement>(SqlAssert.Parse<DropDomainStatement>("drop domain sch.d restrict"));
        new EmptyVisitor().Visit(domain);
    }

    [Fact]
    public void Parses_create_type()
    {
        var distinct = SqlAssert.Parse<CreateTypeStatement>("create type emp_id as integer final");
        Assert.NotNull(distinct.Representation);
        Assert.Empty(distinct.Representation.Collections);
        Assert.NotNull(distinct.FinalKeyword);
        Assert.Null(distinct.Attributes);

        var array = SqlAssert.Parse<CreateTypeStatement>("create type phones as character varying(20) array[5]");
        Assert.Equal(SyntaxKind.ArrayKeyword, Assert.Single(array.Representation!.Collections).Keyword.Kind);

        var multiset = SqlAssert.Parse<CreateTypeStatement>("create type tags as character varying(20) multiset final");
        Assert.Equal(SyntaxKind.MultisetKeyword, Assert.Single(multiset.Representation!.Collections).Keyword.Kind);

        var structured = SqlAssert.Parse<CreateTypeStatement>("""
            create type sch.person under being as (
              name character varying(40) references are checked on delete cascade,
              age integer default 0
            ) not instantiable not final
              ref using integer
              cast (source as ref) with to_ref
              cast (ref as source) with from_ref
              static method make(name character varying(40)) returns person,
              overriding method label() returns character varying(20),
              method score(in n integer) returns integer
                specific sch.score
                self as result
                language sql
                deterministic
                reads sql data
                called on null input
            """);
        Assert.Equal(Count.Two, structured.Name.Count);
        Assert.NotNull(structured.UnderKeyword);
        Assert.Equal(Count.Two, structured.Attributes!.Count);
        Assert.NotNull(structured.Attributes[0].OnDelete);
        Assert.NotNull(structured.Attributes[1].Default);
        Assert.NotNull(structured.NotInstantiableKeyword);
        Assert.NotNull(structured.NotFinalKeyword);
        Assert.NotNull(structured.Reference!.UsingKeyword);
        Assert.Equal(Count.Two, structured.Casts.Count);
        Assert.Equal(Count.Three, structured.Methods.Count);
        Assert.NotNull(structured.Methods[0].Kind);
        Assert.NotNull(structured.Methods[1].OverridingKeyword);
        Assert.NotNull(structured.Methods[Count.Two].SpecificName);
        Assert.Equal(Count.Four, structured.Methods[Count.Two].Characteristics.Count);
        new EmptyVisitor().Visit(structured);

        var derived = SqlAssert.Parse<CreateTypeStatement>("create type t as (id integer) final ref from (id)");
        Assert.NotNull(derived.Reference!.FromKeyword);
        Assert.Single(derived.Reference.Attributes!);

        var system = SqlAssert.Parse<CreateTypeStatement>("create type t as (id integer) not final ref is system generated");
        Assert.NotNull(system.Reference!.GeneratedKeyword);
        Assert.IsType<DropTypeStatement>(SqlAssert.Parse<DropTypeStatement>("drop type sch.person restrict"));
    }

    [Fact]
    public void Parses_ordering_cast_and_transform()
    {
        var state = SqlAssert.Parse<CreateOrderingStatement>("create ordering for person equals only by state");
        Assert.NotNull(state.EqualsKeyword);
        Assert.Equal(SyntaxKind.OnlyKeyword, state.Form.Kind);
        Assert.Null(state.Routine);

        var map = SqlAssert.Parse<CreateOrderingStatement>("create ordering for sch.person order full by map with function person_map");
        Assert.NotNull(map.OrderKeyword);
        Assert.Equal(SyntaxKind.FullKeyword, map.Form.Kind);
        Assert.NotNull(map.Routine);

        var relative = SqlAssert.Parse<CreateOrderingStatement>("create ordering for person order full by relative with specific function sch.rel");
        Assert.NotNull(relative.Routine!.SpecificKeyword);

        var cast = SqlAssert.Parse<CreateCastStatement>("create cast (integer as emp_id) with function int_to_emp as assignment");
        Assert.NotNull(cast.Source);
        Assert.NotNull(cast.AssignmentKeyword);
        var specific = SqlAssert.Parse<CreateCastStatement>("create cast (emp_id as integer) with specific function sch.to_int");
        Assert.NotNull(specific.Routine.SpecificKeyword);
        Assert.Null(specific.AssignmentKeyword);

        var transform = SqlAssert.Parse<CreateTransformStatement>("create transform for address my_group (to sql with function addr_to_sql, from sql with specific function sch.from_sql)");
        var group = Assert.Single(transform.Groups);
        Assert.Equal(Count.Two, group.Elements.Count);
        var groups = SqlAssert.Parse<CreateTransformStatement>("create transforms for address g1 (to sql with function f) g2 (from sql with function g)");
        Assert.Equal(Count.Two, groups.Groups.Count);
        new EmptyVisitor().Visit(cast);
        new EmptyVisitor().Visit(state);
        new EmptyVisitor().Visit(transform);
    }

    [Fact]
    public void Parses_assertion()
    {
        var created = SqlAssert.Parse<CreateAssertionStatement>("create assertion sch.a check (value > 0) deferrable initially deferred");
        Assert.Equal(Count.Two, created.Name.Count);
        Assert.NotNull(created.InitiallyWhen);
        Assert.IsType<DropAssertionStatement>(SqlAssert.Parse<DropAssertionStatement>("drop assertion a cascade"));
        new EmptyVisitor().Visit(created);
    }

    [Fact]
    public void Parses_character_set_collation_and_translation()
    {
        var charset = SqlAssert.Parse<CreateCharacterSetStatement>("create character set sch.latin as get utf8 collate sch.en");
        Assert.NotNull(charset.AsKeyword);
        Assert.Equal(SyntaxKind.SetKeyword, charset.SetKeyword.Kind);
        Assert.Equal(Count.Two, charset.Collation!.Count);
        Assert.IsType<DropCharacterSetStatement>(SqlAssert.Parse<DropCharacterSetStatement>("drop character set latin"));

        var collation = SqlAssert.Parse<CreateCollationStatement>("create collation sch.en for utf8 from ucs_basic no pad");
        Assert.NotNull(collation.NoKeyword);
        Assert.NotNull(collation.PadKeyword);
        var padded = SqlAssert.Parse<CreateCollationStatement>("create collation en for latin1 from default_collation pad space");
        Assert.NotNull(padded.SpaceKeyword);
        Assert.IsType<DropCollationStatement>(SqlAssert.Parse<DropCollationStatement>("drop collation en restrict"));

        var existing = SqlAssert.Parse<CreateTranslationStatement>("create translation sch.t for utf8 to latin1 from other_trans");
        Assert.NotNull(existing.Existing);
        Assert.Null(existing.Routine);
        var routine = SqlAssert.Parse<CreateTranslationStatement>("create translation t for utf8 to latin1 from specific function sch.trans");
        Assert.NotNull(routine.Routine);
        Assert.IsType<DropTranslationStatement>(SqlAssert.Parse<DropTranslationStatement>("drop translation t"));
        new EmptyVisitor().Visit(charset);
        new EmptyVisitor().Visit(collation);
        new EmptyVisitor().Visit(routine);
    }

    [Fact]
    public void Parses_sequence_index_and_comment()
    {
        var sequence = SqlAssert.Parse<CreateSequenceStatement>("create sequence sch.s as integer start with 1 increment by 1 minvalue 1 maxvalue 100 cycle no minvalue no maxvalue no cycle");
        Assert.NotNull(sequence.DataType);
        Assert.Equal(Count.Eight, sequence.Options.Count);
        Assert.IsType<DropSequenceStatement>(SqlAssert.Parse<DropSequenceStatement>("drop sequence s cascade"));

        var index = SqlAssert.Parse<CreateIndexStatement>("create unique clustered index sch.i on s.t (a desc nulls last, b) where a > 0");
        Assert.NotNull(index.UniqueKeyword);
        Assert.NotNull(index.ClusteredKeyword);
        Assert.Equal(Count.Two, index.Columns.Count);
        Assert.NotNull(index.Columns[0].NullsOrder);
        Assert.NotNull(index.Where);
        var renamed = SqlAssert.Parse<AlterIndexStatement>("alter index i rename to j");
        Assert.NotNull(renamed.NewName);
        var rebuilt = SqlAssert.Parse<AlterIndexStatement>("alter index i on t rebuild");
        Assert.NotNull(rebuilt.Action);
        var dropped = SqlAssert.Parse<DropIndexStatement>("drop index i on t restrict");
        Assert.NotNull(dropped.TableName);
        Assert.NotNull(dropped.Behavior);

        var comment = SqlAssert.Parse<CommentStatement>("comment on column s.t.a is 'name'");
        Assert.Equal(Count.Three, comment.Name.Count);
        Assert.Equal(SyntaxKind.String, comment.Value.Kind);
        var cleared = SqlAssert.Parse<CommentStatement>("comment on table t is null");
        Assert.Equal(SyntaxKind.NullKeyword, cleared.Value.Kind);
        new EmptyVisitor().Visit(sequence);
        new EmptyVisitor().Visit(index);
        new EmptyVisitor().Visit(comment);
    }

    [Fact]
    public void Parses_privileges_and_roles()
    {
        var grant = SqlAssert.Parse<GrantPrivilegeStatement>("grant select, insert (a, b), update (a), delete, references (a) on table s.t to u, public with hierarchy option with grant option granted by current_role");
        Assert.Equal(Count.Five, grant.Actions.Count);
        Assert.Equal(Count.Two, grant.Actions.Single(action => action.Columns is { Count: Count.Two }).Columns!.Count);
        Assert.Equal(Count.Two, grant.Grantees.Count);
        Assert.NotNull(grant.HierarchyOption);
        Assert.NotNull(grant.GrantOption);
        Assert.NotNull(grant.Grantor);

        var all = SqlAssert.Parse<GrantPrivilegeStatement>("grant all privileges on t to current_user with grant option");
        Assert.NotNull(Assert.Single(all.Actions).PrivilegesKeyword);
        var domain = SqlAssert.Parse<GrantPrivilegeStatement>("grant usage on domain d to public");
        Assert.NotNull(domain.Object.Kind);
        var routine = SqlAssert.Parse<GrantPrivilegeStatement>("grant execute on specific function sch.f to u");
        Assert.NotNull(routine.Object.Routine);
        var charset = SqlAssert.Parse<GrantPrivilegeStatement>("grant usage on character set utf8 to public");
        Assert.NotNull(charset.Object.SetKeyword);

        var roles = SqlAssert.Parse<GrantRoleStatement>("grant r1, r2 to u, public with admin option granted by current_user");
        Assert.Equal(Count.Two, roles.Roles.Count);
        Assert.NotNull(roles.AdminOption);

        var revoked = SqlAssert.Parse<RevokePrivilegeStatement>("revoke grant option for select on t from u cascade");
        Assert.NotNull(revoked.Option);
        var revokedRole = SqlAssert.Parse<RevokeRoleStatement>("revoke admin option for r1, r2 from u restrict");
        Assert.Equal(Count.Two, revokedRole.Roles.Count);
        Assert.IsType<RevokeRoleStatement>(SqlAssert.Parse<RevokeRoleStatement>("revoke r1 from public cascade"));

        var created = SqlAssert.Parse<CreateRoleStatement>("create role r with admin current_user");
        Assert.NotNull(created.AdminKeyword);
        Assert.NotNull(created.Grantor);
        Assert.IsType<DropRoleStatement>(SqlAssert.Parse<DropRoleStatement>("drop role r"));

        var none = SqlAssert.Parse<SetRoleStatement>("set role none");
        Assert.Equal(SyntaxKind.Identifier, none.Value.Kind);
        Assert.IsType<SetRoleStatement>(SqlAssert.Parse<SetRoleStatement>("set role admin"));
        new EmptyVisitor().Visit(grant);
        new EmptyVisitor().Visit(roles);
        new EmptyVisitor().Visit(revoked);
        new EmptyVisitor().Visit(created);
        new EmptyVisitor().Visit(none);
    }

    [Fact]
    public void Parses_transactions_and_sessions()
    {
        var started = SqlAssert.Parse<StartTransactionStatement>("start transaction isolation level read committed, read only, diagnostics size 10");
        Assert.Equal(Count.Three, started.Modes.Count);
        Assert.NotNull(started.Modes.Last().Size);
        var serializable = SqlAssert.Parse<StartTransactionStatement>("start transaction isolation level serializable");
        Assert.NotNull(Assert.Single(serializable.Modes).Level);
        var repeatable = SqlAssert.Parse<SetTransactionStatement>("set local transaction isolation level repeatable read, read write");
        Assert.NotNull(repeatable.LocalKeyword);
        Assert.Equal(Count.Two, repeatable.Modes.Count);
        Assert.IsType<SetTransactionStatement>(SqlAssert.Parse<SetTransactionStatement>("set transaction isolation level read uncommitted"));

        var commit = SqlAssert.Parse<CommitStatement>("commit work and no chain");
        Assert.NotNull(commit.WorkKeyword);
        Assert.NotNull(commit.NoKeyword);
        Assert.NotNull(commit.ChainKeyword);
        Assert.IsType<CommitStatement>(SqlAssert.Parse<CommitStatement>("commit and chain"));

        var rollback = SqlAssert.Parse<RollbackStatement>("rollback work and chain");
        Assert.NotNull(rollback.AndKeyword);
        var toSavepoint = SqlAssert.Parse<RollbackStatement>("rollback to savepoint s");
        Assert.NotNull(toSavepoint.SavepointKeyword);
        Assert.IsType<SavepointStatement>(SqlAssert.Parse<SavepointStatement>("savepoint s"));
        Assert.IsType<ReleaseSavepointStatement>(SqlAssert.Parse<ReleaseSavepointStatement>("release savepoint s"));

        var constraints = SqlAssert.Parse<SetConstraintsStatement>("set constraints sch.c, d immediate");
        Assert.Equal(Count.Two, constraints.Names!.Count);
        Assert.NotNull(SqlAssert.Parse<SetConstraintsStatement>("set constraints all deferred").AllKeyword);

        var authorization = SqlAssert.Parse<SetSessionAuthorizationStatement>("set session authorization 'u'");
        Assert.Equal(SyntaxKind.String, authorization.Value.Kind);
        var characteristics = SqlAssert.Parse<SetSessionCharacteristicsStatement>("set session characteristics as isolation level serializable, read only");
        Assert.Equal(Count.Two, characteristics.Modes.Count);
        var names = SqlAssert.Parse<SetNamesStatement>("set names 'utf8' collate sch.en");
        Assert.NotNull(names.Collation);
        Assert.IsType<SetCharacterSetStatement>(SqlAssert.Parse<SetCharacterSetStatement>("set character set utf8"));
        Assert.Equal(Count.Two, SqlAssert.Parse<SetCollationStatement>("set collation sch.en").Name!.Count);

        Assert.NotNull(SqlAssert.Parse<SetTimeZoneStatement>("set time zone local").LocalKeyword);
        var zone = SqlAssert.Parse<SetTimeZoneStatement>("set time zone interval '1' hour");
        Assert.NotNull(zone.Value);
        Assert.IsType<SetCatalogStatement>(SqlAssert.Parse<SetCatalogStatement>("set catalog cat"));
        Assert.IsType<SetSchemaStatement>(SqlAssert.Parse<SetSchemaStatement>("set schema s"));
        Assert.Equal(Count.Two, SqlAssert.Parse<SetPathStatement>("set path a, b.s").Names!.Count);

        new EmptyVisitor().Visit(started);
        new EmptyVisitor().Visit(repeatable);
        new EmptyVisitor().Visit(commit);
        new EmptyVisitor().Visit(toSavepoint);
        new EmptyVisitor().Visit(characteristics);
        new EmptyVisitor().Visit(zone);
    }

    [Fact]
    public void Parses_connections_and_cursors()
    {
        var connect = SqlAssert.Parse<ConnectStatement>("connect to 'server' as c user 'u'");
        Assert.NotNull(connect.AsKeyword);
        Assert.NotNull(connect.UserKeyword);
        Assert.IsType<ConnectStatement>(SqlAssert.Parse<ConnectStatement>("connect to default"));
        Assert.IsType<DisconnectStatement>(SqlAssert.Parse<DisconnectStatement>("disconnect all"));
        Assert.IsType<DisconnectStatement>(SqlAssert.Parse<DisconnectStatement>("disconnect current"));
        Assert.IsType<SetConnectionStatement>(SqlAssert.Parse<SetConnectionStatement>("set connection default"));

        var cursor = SqlAssert.Parse<DeclareCursorStatement>("declare c sensitive scroll cursor with hold with return for select 1 for update");
        Assert.NotNull(cursor.Sensitivity);
        Assert.NotNull(cursor.Scroll);
        Assert.NotNull(cursor.Hold);
        Assert.NotNull(cursor.ReturnKeyword);
        Assert.IsType<SelectStatement>(cursor.Query);
        var hidden = SqlAssert.Parse<DeclareCursorStatement>("declare c insensitive no scroll cursor without hold without return for select 1");
        Assert.NotNull(hidden.NoScroll);
        Assert.NotNull(hidden.HoldWith);
        Assert.IsType<DeclareCursorStatement>(SqlAssert.Parse<DeclareCursorStatement>("declare c asensitive cursor for select 1"));

        Assert.IsType<OpenStatement>(SqlAssert.Parse<OpenStatement>("open c"));
        var fetch = SqlAssert.Parse<FetchStatement>("fetch absolute 1 from c into a, b");
        Assert.NotNull(fetch.Offset);
        Assert.Equal(Count.Two, fetch.Targets.Count);
        Assert.IsType<FetchStatement>(SqlAssert.Parse<FetchStatement>("fetch next from c into :x"));
        Assert.IsType<CloseStatement>(SqlAssert.Parse<CloseStatement>("close c"));

        Assert.NotNull(SqlAssert.Parse<UpdateStatement>("update t set a = 1 where current of c").Positioned);
        Assert.NotNull(SqlAssert.Parse<DeleteStatement>("delete from t where current of c").Positioned);

        var allocated = SqlAssert.Parse<AllocateCursorStatement>("allocate c insensitive scroll cursor for stmt");
        Assert.NotNull(allocated.Sensitivity);
        Assert.IsType<DeallocateStatement>(SqlAssert.Parse<DeallocateStatement>("deallocate prepare stmt"));
        Assert.IsType<DeallocateStatement>(SqlAssert.Parse<DeallocateStatement>("deallocate descriptor d"));

        new EmptyVisitor().Visit(connect);
        new EmptyVisitor().Visit(cursor);
        new EmptyVisitor().Visit(fetch);
        new EmptyVisitor().Visit(allocated);
    }

    [Fact]
    public void Parses_dynamic_sql_and_diagnostics()
    {
        var prepared = SqlAssert.Parse<PrepareStatement>("prepare global stmt from :sql");
        Assert.NotNull(prepared.Scope);
        var executed = SqlAssert.Parse<ExecuteStatement>("execute stmt into a, b using sql descriptor d");
        Assert.Equal(Count.Two, executed.Targets!.Count);
        Assert.NotNull(executed.UsingDescriptor);
        Assert.IsType<ExecuteImmediateStatement>(SqlAssert.Parse<ExecuteImmediateStatement>("execute immediate 'select 1'"));

        var described = SqlAssert.Parse<DescribeStatement>("describe input stmt using sql descriptor d");
        Assert.NotNull(described.InputOrOutput);
        Assert.IsType<DescribeStatement>(SqlAssert.Parse<DescribeStatement>("describe output stmt into descriptor d"));

        var dynamic = SqlAssert.Parse<DynamicDeclareCursorStatement>("declare c sensitive scroll cursor with hold for stmt");
        Assert.NotNull(dynamic.Sensitivity);
        Assert.NotNull(dynamic.Hold);
        var descriptor = SqlAssert.Parse<AllocateDescriptorStatement>("allocate sql descriptor d with max 10");
        Assert.NotNull(descriptor.SqlKeyword);
        Assert.NotNull(descriptor.MaxKeyword);

        var diagnostics = SqlAssert.Parse<GetDiagnosticsStatement>("get diagnostics exception 1 :state = returned_sqlstate, :msg = message_text");
        Assert.Equal(Count.Two, diagnostics.Items.Count);
        Assert.IsType<GetDiagnosticsStatement>(SqlAssert.Parse<GetDiagnosticsStatement>("get diagnostics :rows = row_count"));

        var signal = SqlAssert.Parse<SignalStatement>("signal sqlstate value '45000' set message_text = 'failed'");
        Assert.NotNull(signal.ValueKeyword);
        Assert.NotNull(signal.SetKeyword);
        var resignal = SqlAssert.Parse<ResignalStatement>(BareResignal);
        Assert.Empty(resignal.Items);
        Assert.IsType<ResignalStatement>(SqlAssert.Parse<ResignalStatement>("resignal condition_name set message_text = 'x'"));

        new EmptyVisitor().Visit(prepared);
        new EmptyVisitor().Visit(executed);
        new EmptyVisitor().Visit(dynamic);
        new EmptyVisitor().Visit(diagnostics);
        new EmptyVisitor().Visit(signal);
        new EmptyVisitor().Visit(resignal);
    }

    [Fact]
    public void Parses_triggers_and_routines()
    {
        var before = SqlAssert.Parse<CreateTriggerStatement>("create trigger sch.tr before insert on t referencing old row as o new row as n for each row when (a > 0) insert into u values (1)");
        Assert.NotNull(before.ReferencingKeyword);
        Assert.Equal(Count.Two, before.Transitions.Count);
        Assert.NotNull(before.When);
        Assert.IsType<InsertStatement>(before.Body);
        var after = SqlAssert.Parse<CreateTriggerStatement>("create trigger tr after update of a, b on t for each statement delete from u");
        Assert.Equal(Count.Two, after.Columns!.Count);
        Assert.NotNull(after.Granularity);
        var instead = SqlAssert.Parse<CreateTriggerStatement>("create trigger tr instead of delete on v update t set a = 1");
        Assert.NotNull(instead.OfKeyword);
        Assert.IsType<DropTriggerStatement>(SqlAssert.Parse<DropTriggerStatement>("drop trigger sch.tr"));

        var function = SqlAssert.Parse<CreateRoutineStatement>("create function sch.f(x integer) returns integer language sql deterministic returns null on null input sql return x + 1");
        Assert.NotNull(function.ReturnsType);
        Assert.Equal(Count.Three, function.Characteristics.Count);
        Assert.IsType<ReturnStatement>(function.Body);
        var procedure = SqlAssert.Parse<CreateRoutineStatement>("create procedure p() modifies sql data sql insert into t values (1)");
        Assert.Null(procedure.ReturnsKeyword);
        Assert.NotNull(procedure.SqlKeyword);
        var method = SqlAssert.Parse<CreateRoutineStatement>("create static method age() returns integer for person language c external name 'age'");
        Assert.NotNull(method.KindPrefix);
        Assert.NotNull(method.ForType);
        Assert.NotNull(method.ExternalName);

        var altered = SqlAssert.Parse<AlterRoutineStatement>("alter specific function sch.f language sql contains sql");
        Assert.Equal(Count.Two, altered.Characteristics.Count);
        Assert.IsType<DropRoutineStatement>(SqlAssert.Parse<DropRoutineStatement>("drop procedure p cascade"));
        var called = SqlAssert.Parse<CallStatement>("call sch.p(1, a)");
        Assert.Equal(Count.Two, called.Arguments.Count);
        var returned = SqlAssert.Parse<ReturnStatement>("return null");
        Assert.NotNull(returned.Value);

        new EmptyVisitor().Visit(before);
        new EmptyVisitor().Visit(function);
        new EmptyVisitor().Visit(method);
        new EmptyVisitor().Visit(called);
        new EmptyVisitor().Visit(returned);
    }

    [Fact]
    public void Parses_psm()
    {
        var compound = SqlAssert.Parse<CompoundStatement>("""
            label: begin atomic
                declare a, b integer default 0;
                declare not_found condition for sqlstate '02000';
                declare continue handler for sqlexception, not found
                    set a = 1;
                set (a, b) = (1, 2);
                if a > 0 then
                    set a = 2;
                elseif a < 0 then
                    set a = 3;
                else
                    set a = 4;
                end if;
                case a
                    when 1 then set a = 5;
                else
                    set a = 6;
                end case;
                loop
                    leave label;
                end loop;
                while a > 0 do
                    iterate label;
                end while;
                repeat
                    set a = a;
                until a > 0
                end repeat;
                for x as c cursor for select a from t where true do
                    set a = 1;
                end for;
                signal sqlstate '45000';
            end label
            """);
        Assert.NotNull(compound.Label);
        Assert.NotNull(compound.AtomicKeyword);
        Assert.Single(compound.Statements.OfType<DeclareVariableStatement>());
        Assert.Single(compound.Statements.OfType<DeclareConditionStatement>());
        Assert.Single(compound.Statements.OfType<DeclareHandlerStatement>());
        Assert.Single(compound.Statements.OfType<SetAssignmentStatement>());
        var ifStatement = Assert.Single(compound.Statements.OfType<IfStatement>());
        Assert.NotNull(ifStatement.ElseKeyword);
        Assert.Single(ifStatement.ElseIfs);
        Assert.Single(compound.Statements.OfType<CaseStatement>());
        Assert.Single(compound.Statements.OfType<LoopStatement>());
        Assert.Single(compound.Statements.OfType<WhileStatement>());
        Assert.Single(compound.Statements.OfType<RepeatStatement>());
        Assert.Single(compound.Statements.OfType<ForStatement>());
        Assert.Single(compound.Statements.OfType<SignalStatement>());
        new EmptyVisitor().Visit(compound);
    }
}
