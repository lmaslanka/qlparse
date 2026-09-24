namespace QlParse.Tests;

public sealed class ParseQueryTests
{
    private sealed class EmptyVisitor : SqlVisitor;


    [Fact]
    public void Parses_insert_select()
    {
        var insert = SqlAssert.Parse<InsertStatement>("insert into t select 1");
        Assert.Null(insert.Columns);
        Assert.IsType<SelectStatement>(insert.Query);
    }

    [Fact]
    public void Parses_insert_select_column_list()
    {
        var insert = SqlAssert.Parse<InsertStatement>("insert into t (a, b) select 1, 2");
        Assert.Equal(Count.Two, insert.Columns!.Count);
    }

    [Fact]
    public void Parses_insert_select_with_at_parameter()
    {
        var sql = """
            INSERT INTO activity_feed (
                    audit_event_id,
                    occurred_on,
                    actor_kind,
                    actor_user_id,
                    actor_auth0_id,
                    actor_display_name,
                    subject_type,
                    subject_record_id,
                    subject_label,
                    parent_subject_type,
                    parent_subject_record_id,
                    action
                )
                     SELECT record_id,
                            occurred_on,
                            actor_kind,
                            actor_user_id,
                            actor_auth0_id,
                            actor_display_name,
                            subject_type,
                            subject_record_id,
                            subject_label,
                            parent_subject_type,
                            parent_subject_record_id,
                            action
                       FROM audit_event
                      WHERE record_id = @auditEventId;
            """;
        var result = Sql.Parse(sql, SqlFlags.AtParameters);
        Assert.Null(result.Error);
        var insert = Assert.IsType<InsertStatement>(result.Root);
        Assert.IsType<SelectStatement>(insert.Query);
        new EmptyVisitor().Visit(insert);
    }

    [Fact]
    public void Parses_values()
    {
        var query = SqlAssert.Parse<ValuesQuery>("values (1), (2, 3)");
        Assert.Equal(Count.Two, query.Rows.Count);
        Assert.Single(query.Rows[0].Values);
        Assert.Equal(Count.Two, query.Rows[1].Values.Count);
    }

    [Fact]
    public void Parses_paren_query()
    {
        var query = SqlAssert.Parse<ParenQuery>("(select 1)");
        Assert.IsType<SelectStatement>(query.Inner);
    }

    [Fact]
    public void Parses_with()
    {
        var query = SqlAssert.Parse<WithQuery>("with cte as (select 1) select * from cte");
        Assert.Null(query.RecursiveKeyword);
        Assert.Single(query.Ctes);
        Assert.Null(query.Ctes[0].Columns);
        Assert.IsType<SelectStatement>(query.Query);
    }

    [Fact]
    public void Parses_with_recursive_and_columns()
    {
        var query = SqlAssert.Parse<WithQuery>("with recursive cte (x, y) as (select 1, 2) select * from cte");
        Assert.NotNull(query.RecursiveKeyword);
        Assert.Equal(Count.Two, query.Ctes[0].Columns!.Count);
    }

    [Fact]
    public void Parses_search_and_cycle()
    {
        var depth = SqlAssert.Parse<WithQuery>("with recursive cte as (select 1) search depth first by id set seq select * from cte");
        var search = depth.Ctes[0].Search;
        Assert.NotNull(search);
        Assert.Null(depth.Ctes[0].Cycle);
        Assert.Single(search.Columns);

        var breadth = SqlAssert.Parse<WithQuery>("with cte as (select 1) search breadth first by a, b set seq select * from cte");
        Assert.Equal(Count.Two, breadth.Ctes[0].Search!.Columns.Count);

        var cycle = SqlAssert.Parse<WithQuery>("with recursive cte as (select 1) cycle id set is_cycle to true default false using path select * from cte");
        Assert.Null(cycle.Ctes[0].Search);
        var cycleClause = cycle.Ctes[0].Cycle;
        Assert.NotNull(cycleClause);
        Assert.IsType<LiteralExpression>(cycleClause.MarkValue);

        var both = SqlAssert.Parse<WithQuery>("with recursive cte as (select 1) search depth first by id set seq cycle id, parent set is_cycle to 'Y' default 'N' using path select * from cte");
        Assert.NotNull(both.Ctes[0].Search);
        Assert.Equal(Count.Two, both.Ctes[0].Cycle!.Columns.Count);
    }

    [Fact]
    public void Parses_linear_and_general_recursion()
    {
        var linear = SqlAssert.Parse<WithQuery>("with recursive linear cte as (select 1) select * from cte");
        Assert.NotNull(linear.RecursionLimit);

        var general = SqlAssert.Parse<WithQuery>("with recursive general cte as (select 1) select * from cte");
        Assert.NotNull(general.RecursionLimit);
        Assert.NotNull(general.RecursiveKeyword);
    }

    [Fact]
    public void Parses_multiple_ctes()
    {
        var query = SqlAssert.Parse<WithQuery>("with a as (select 1), b as (select 2) select * from a");
        Assert.Equal(Count.Two, query.Ctes.Count);
    }

    [Fact]
    public void Parses_union()
    {
        var op = SqlAssert.Parse<SetOperation>("select 1 union select 2");
        Assert.Equal(SyntaxKind.UnionKeyword, op.Operator.Kind);
        Assert.Null(op.AllKeyword);
        Assert.IsType<SelectStatement>(op.Left);
        Assert.IsType<SelectStatement>(op.Right);
    }

    [Fact]
    public void Parses_union_all()
    {
        var op = SqlAssert.Parse<SetOperation>("select 1 union all select 2");
        Assert.NotNull(op.AllKeyword);
    }

    [Fact]
    public void Parses_except_and_intersect()
    {
        Assert.Equal(SyntaxKind.ExceptKeyword, SqlAssert.Parse<SetOperation>("select 1 except select 2").Operator.Kind);
        Assert.Equal(SyntaxKind.IntersectKeyword, SqlAssert.Parse<SetOperation>("select 1 intersect select 2").Operator.Kind);
    }

    [Fact]
    public void Intersect_binds_tighter_than_union()
    {
        var op = SqlAssert.Parse<SetOperation>("select 1 union select 2 intersect select 3");
        Assert.Equal(SyntaxKind.UnionKeyword, op.Operator.Kind);
        var intersect = Assert.IsType<SetOperation>(op.Right);
        Assert.Equal(SyntaxKind.IntersectKeyword, intersect.Operator.Kind);
    }

    [Fact]
    public void Union_is_left_associative()
    {
        var op = SqlAssert.Parse<SetOperation>("select 1 union select 2 union select 3");
        Assert.Equal(SyntaxKind.UnionKeyword, op.Operator.Kind);
        Assert.IsType<SetOperation>(op.Left);
        Assert.IsType<SelectStatement>(op.Right);
    }

    [Fact]
    public void Parses_corresponding()
    {
        var op = SqlAssert.Parse<SetOperation>("select a from t union corresponding select a from u");
        Assert.NotNull(op.CorrespondingKeyword);
        Assert.Null(op.Columns);
    }

    [Fact]
    public void Parses_corresponding_by()
    {
        var op = SqlAssert.Parse<SetOperation>("select a from t union corresponding by (a, b) select a from u");
        Assert.NotNull(op.ByKeyword);
        Assert.Equal(Count.Two, op.Columns!.Count);
    }

    [Fact]
    public void Optional_semicolon()
    {
        Assert.IsType<SelectStatement>(Sql.Parse("select 1;").Root);
    }

    [Fact]
    public void Extra_tokens_throw()
    {
        Assert.NotNull(Sql.Parse("select 1 2").Error);
    }

    [Fact]
    public void Extra_semicolon_throws()
    {
        Assert.NotNull(Sql.Parse("select 1;;").Error);
    }

    [Fact]
    public void Parses_direct_sql_script()
    {
        var script = SqlAssert.Parse<DirectSqlScript>("select 1; select 2; select 3");
        Assert.Equal(Count.Three, script.Statements.Count);
        Assert.Equal(Count.Two, script.Semicolons.Count);
        Assert.IsType<SelectStatement>(script.Statements[0]);
        var terminated = SqlAssert.Parse<DirectSqlScript>("select 1; insert into t values (1);");
        Assert.Equal(Count.Two, terminated.Statements.Count);
        new EmptyVisitor().Visit(script);
    }

    [Fact]
    public void Parses_sql_client_module()
    {
        var module = SqlAssert.Parse<ModuleDefinition>("""
            module mod1 names are latin1 language c schema sch authorization u path sch, other
            declare c cursor for select 1;
            procedure p (x integer);
            select x;
            """);
        Assert.NotNull(module.NamesKeyword);
        Assert.NotNull(module.SchemaKeyword);
        Assert.NotNull(module.Authorization);
        Assert.Equal(Count.Two, module.Path.Count);
        Assert.Equal(Count.Two, module.Contents.Count);
        Assert.IsType<ModuleProcedure>(module.Contents[1]);
        new EmptyVisitor().Visit(module);
    }

    [Fact]
    public void Parses_embedded_sql_and_declare_section()
    {
        var embedded = SqlAssert.Parse<EmbeddedSqlStatement>("exec sql select 1;");
        Assert.IsType<SelectStatement>(embedded.Statement);
        Assert.NotNull(embedded.Semicolon);
        var endExec = SqlAssert.Parse<EmbeddedSqlStatement>("exec sql select 1 end-exec");
        Assert.NotNull(endExec.EndExec);
        var begin = Assert.IsType<DeclareSectionStatement>(SqlAssert.Parse<EmbeddedSqlStatement>("exec sql begin declare section;").Statement);
        Assert.Equal(SyntaxKind.Identifier, begin.BeginOrEnd.Kind);
        Assert.Equal(SyntaxKind.EndKeyword, SqlAssert.Parse<DeclareSectionStatement>("end declare section").BeginOrEnd.Kind);
        var whenever = Assert.IsType<WheneverStatement>(SqlAssert.Parse<EmbeddedSqlStatement>("exec sql whenever not found go to missing;").Statement);
        Assert.NotNull(whenever.NotKeyword);
        Assert.NotNull(whenever.ToKeyword);
        Assert.IsType<WheneverStatement>(SqlAssert.Parse<WheneverStatement>("whenever sqlerror continue"));
        new EmptyVisitor().Visit(embedded);
    }

    [Fact]
    public void Information_schema_is_ordinary_identifiers()
    {
        Assert.Equal(SyntaxKind.Identifier, SqlAssert.Kinds("information_schema ")[0]);
        var table = Assert.IsType<TableReference>(SqlAssert.Select("select table_name from information_schema.tables").From);
        Assert.Equal(Count.Two, table.NameParts.Count);
    }

    [Fact]
    public void Parse_null_string_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Sql.Parse((string)null!));
    }
}
