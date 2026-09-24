namespace QlParse.Tests;

public sealed class ParseDmlTests
{
    private sealed class EmptyVisitor : SqlVisitor;

    [Fact]
    public void Parses_insert_values()
    {
        var insert = SqlAssert.Parse<InsertStatement>("insert into t values (1), (2, 3)");
        var values = Assert.IsType<ValuesQuery>(insert.Query);
        Assert.Equal(Count.Two, values.Rows.Count);
        Assert.Null(insert.DefaultKeyword);
    }

    [Fact]
    public void Parses_insert_values_column_list()
    {
        var insert = SqlAssert.Parse<InsertStatement>("insert into t (a, b) values (1, 2)");
        Assert.Equal(Count.Two, insert.Columns!.Count);
        Assert.IsType<ValuesQuery>(insert.Query);
    }

    [Fact]
    public void Parses_insert_default_values()
    {
        var insert = SqlAssert.Parse<InsertStatement>("insert into s.t default values");
        Assert.NotNull(insert.DefaultKeyword);
        Assert.NotNull(insert.ValuesKeyword);
        Assert.Null(insert.Query);
        Assert.Equal(Count.Two, insert.TableName.Count);
    }

    [Fact]
    public void Parses_insert_overriding()
    {
        var user = SqlAssert.Parse<InsertStatement>("insert into t overriding user value values (1)");
        Assert.Equal(SyntaxKind.UserKeyword, user.Override!.Kind.Kind);
        Assert.IsType<ValuesQuery>(user.Query);

        var system = SqlAssert.Parse<InsertStatement>("insert into t (a, b) overriding system value select 1, 2");
        Assert.Equal(Count.Two, system.Columns!.Count);
        Assert.NotNull(system.Override);
        Assert.IsType<SelectStatement>(system.Query);
    }

    [Fact]
    public void Parses_update_searched()
    {
        var update = SqlAssert.Parse<UpdateStatement>("update only (s.t) as x set a = 1, b = 2 where a > 0");
        Assert.NotNull(update.Target.OnlyKeyword);
        Assert.NotNull(update.Target.AsKeyword);
        Assert.Equal(Count.Two, update.Assignments.Count);
        Assert.NotNull(update.Where);
        Assert.Null(update.Positioned);
        new EmptyVisitor().Visit(update);
    }

    [Fact]
    public void Parses_update_positioned()
    {
        var update = SqlAssert.Parse<UpdateStatement>("update t set a = 1 where current of c1");
        Assert.Null(update.Where);
        Assert.NotNull(update.Positioned);
        Assert.Equal(SyntaxKind.OfKeyword, update.Positioned.OfKeyword.Kind);
    }

    [Fact]
    public void Parses_set_row_assignments()
    {
        var update = SqlAssert.Parse<UpdateStatement>("update t set a = default, a[1] = 2, (b, c) = (1, 2), (d, e) = (select x, y from u)");
        Assert.Equal(Count.Four, update.Assignments.Count);
        Assert.NotNull(update.Assignments[0].DefaultKeyword);
        Assert.NotNull(update.Assignments[1].Targets[0].OpenBracket);
        var row = update.Assignments[Count.Two];
        Assert.NotNull(row.OpenParen);
        Assert.Equal(Count.Two, row.Targets.Count);
        Assert.IsType<RowConstructorExpression>(row.Value);
        Assert.IsType<ScalarSubqueryExpression>(update.Assignments[Count.Three].Value);
        new EmptyVisitor().Visit(update);
    }

    [Fact]
    public void Parses_delete_searched()
    {
        var deleted = SqlAssert.Parse<DeleteStatement>("delete from t where a = 1");
        Assert.NotNull(deleted.Where);
        Assert.Null(deleted.Positioned);
        Assert.Null(deleted.Target.Alias);
    }

    [Fact]
    public void Parses_delete_positioned()
    {
        var deleted = SqlAssert.Parse<DeleteStatement>("delete from only (t) as x where current of c1");
        Assert.NotNull(deleted.Target.OnlyKeyword);
        Assert.NotNull(deleted.Target.Alias);
        Assert.NotNull(deleted.Positioned);
        Assert.Null(deleted.Where);
    }

    [Fact]
    public void Parses_merge()
    {
        var merge = SqlAssert.Parse<MergeStatement>("""
            merge into t as tgt
            using a join b on a.id = b.id
            on tgt.id = a.id
            when matched and a.flag = 1 then update set (x, y) = (a.x, a.y), z = default
            when matched then delete
            when not matched by target then insert (x, y) overriding user value values (a.x, a.y)
            when not matched by source and a.id > 0 then delete
            """);
        Assert.NotNull(merge.Target.Alias);
        Assert.Single(merge.SourceJoins);
        Assert.Equal(Count.Four, merge.Whens.Count);
        Assert.IsType<MergeUpdateAction>(merge.Whens[0].Action);
        Assert.IsType<MergeDeleteAction>(merge.Whens[1].Action);
        var insert = Assert.IsType<MergeInsertAction>(merge.Whens[Count.Two].Action);
        Assert.Equal(Count.Two, insert.Columns!.Count);
        Assert.NotNull(insert.Override);
        Assert.NotNull(merge.Whens[Count.Three].ByKeyword);
        Assert.NotNull(merge.Whens[Count.Three].Condition);
        new EmptyVisitor().Visit(merge);
    }

    [Fact]
    public void Parses_merge_derived_source()
    {
        var merge = SqlAssert.Parse<MergeStatement>("merge into t using (select 1 as id) as s on t.id = s.id when not matched then insert values (1)");
        Assert.IsType<DerivedTable>(merge.Source);
        Assert.IsType<MergeInsertAction>(merge.Whens[0].Action);
    }

    [Fact]
    public void Parses_truncate()
    {
        var plain = SqlAssert.Parse<TruncateStatement>("truncate table t");
        Assert.Null(plain.IdentityKeyword);

        var restart = SqlAssert.Parse<TruncateStatement>("truncate table cat.sch.t restart identity");
        Assert.Equal(Count.Three, restart.TableName.Count);
        Assert.NotNull(restart.RestartKeyword);
        Assert.NotNull(restart.IdentityKeyword);

        var continued = SqlAssert.Parse<TruncateStatement>("truncate table t continue identity;");
        Assert.NotNull(continued.IdentityKeyword);
        new EmptyVisitor().Visit(continued);
    }

    [Fact]
    public void Parses_portion_of()
    {
        var update = SqlAssert.Parse<UpdateStatement>("update emp for portion of dept_period from date '2011-01-01' to date '2011-06-01' set name = 'x'");
        Assert.NotNull(update.Portion);
        Assert.Equal(SyntaxKind.Identifier, update.Portion.ToKeyword.Kind);

        var deleted = SqlAssert.Parse<DeleteStatement>("delete from emp for portion of dept_period from date '2011-01-01' to date '2011-06-01' where id = 1");
        Assert.NotNull(deleted.Portion);
        Assert.NotNull(deleted.Where);
        new EmptyVisitor().Visit(update);
    }
}
