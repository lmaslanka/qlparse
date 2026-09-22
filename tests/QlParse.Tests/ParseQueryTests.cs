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
    public void Parse_null_string_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Sql.Parse((string)null!));
    }
}
