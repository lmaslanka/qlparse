namespace QlParse.Tests;

// SQL Server DML (stream C): INSERT / UPDATE / DELETE / MERGE with TOP, OUTPUT, table hints, FROM, compound and
// combined assignments, mutator calls, WHERE CURRENT OF and OPTION; WITH before DML; BULK INSERT; TRUNCATE ...
// WITH (PARTITIONS ...).
public sealed partial class TsqlDmlTests
{
    private const string MergeAllWhens =
        "merge into dbo.t with (holdlock) as tgt using (select 1 as id) as src on tgt.id = src.id "
        + "when matched and tgt.a <> src.a then update set tgt.a = src.a "
        + "when not matched by target then insert (id, a) values (src.id, src.a) "
        + "when not matched by source and tgt.x = 1 then delete "
        + "output $action, inserted.*, deleted.* option (recompile)"
        + Semicolon;

    private const string MergeDefaultValues =
        "merge top (10) t using s on t.id = s.id when not matched then insert default values"
        + Semicolon;

    private const string WithMerge =
        "with c as (select 1 as a) merge t using c on t.a = c.a when matched then delete"
        + Semicolon;

    // ---- INSERT ----

    [Fact]
    public void Parses_insert_without_into()
    {
        const string sql = "insert t values (1)";
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>(sql);
        Assert.Null(insert.IntoKeyword);
        Assert.Equal(["t"], Texts(sql, Assert.IsType<TableReference>(insert.Target).NameParts));
        Assert.IsType<ValuesQuery>(insert.Source);
        Assert.Equal(sql, SpanText(sql, insert.Span));
    }

    [Fact]
    public void Parses_insert_with_columns_and_rows()
    {
        const string sql = "insert into t (a, b) values (1, 2), (3, 4)";
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>(sql);
        Assert.NotNull(insert.IntoKeyword);
        Assert.Equal(["a", "b"], Texts(sql, Ref(insert.Columns)));
        Assert.Equal(Two, Assert.IsType<ValuesQuery>(insert.Source).Rows.Count);
    }

    [Fact]
    public void Parses_insert_top_hints_and_select()
    {
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>("insert top (5) percent into t with (tablock) (a) select a from u");
        var top = Assert.IsType<TsqlTopClause>(insert.Top);
        Assert.NotNull(top.PercentKeyword);
        Assert.Single(Assert.IsType<TsqlHintedTable>(insert.Target).Hints);
        Assert.Single(Ref(insert.Columns));
        Assert.IsType<SelectStatement>(insert.Source);
    }

    [Fact]
    public void Parses_insert_default_values()
    {
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>("insert into t default values");
        Assert.NotNull(insert.DefaultKeyword);
        Assert.NotNull(insert.ValuesKeyword);
        Assert.Null(insert.Source);
    }

    [Fact]
    public void Parses_insert_output_into_table_variable()
    {
        const string sql = "insert into t (a) output inserted.id, inserted.a into @ids (id, a) values (1)";
        var output = Assert.Single(SqlAssert.ParseTsql<TsqlInsertStatement>(sql).Output);
        Assert.Equal(Two, output.Columns.Count);
        Assert.Equal(["@ids"], Texts(sql, Ref(output.Target)));
        Assert.Equal(["id", "a"], Texts(sql, Ref(output.TargetColumns)));
    }

    [Theory]
    [InlineData("insert into @t select 1", "@t")]
    [InlineData("insert into db..t values (1)", "db..t")]
    public void Parses_insert_into_variables_and_omitted_part_names(string sql, string target)
    {
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>(sql);
        Assert.Equal(target, SpanText(sql, insert.Target.Span));
    }

    [Fact]
    public void Parses_insert_into_a_rowset_function()
    {
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>("insert into openquery(srv, 'select a from t') values (1)");
        Assert.IsType<TsqlRowsetFunctionTable>(insert.Target);
    }

    [Fact]
    public void Insert_without_into_is_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("insert t values (1)");
        Assert.IsType<InsertStatement>(Sql.Parse("insert into t values (1)").Root);
    }

    [Fact]
    public void Parses_insert_output_before_exec()
    {
        const string sql = "insert into @t (a) output inserted.a into @log exec p";
        var insert = SqlAssert.ParseTsql<TsqlInsertStatement>(sql);
        Assert.Equal(SyntaxKind.EmbeddedHost, Assert.IsType<TableReference>(insert.Target).NameParts[0].Kind);
        Assert.Equal(["@log"], Texts(sql, Ref(Assert.Single(insert.Output).Target)));
        Assert.IsType<TsqlExecuteStatement>(insert.Source);
    }

    [Fact]
    public void Parses_hinted_table_variable_with_top_in_a_batch()
    {
        const string sql = "declare @t table (a int) insert @t output inserted.a values (1) select top (1) * from @t t with (nolock)";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(Three, batch.Statements.Count);
        var select = Assert.IsType<SelectStatement>(batch.Statements[Two]);
        Assert.NotNull(Extras(select).Top);
        var hinted = Assert.IsType<TsqlHintedTable>(select.From);
        Assert.Equal("@t", Text(sql, Assert.IsType<TableReference>(hinted.Table).NameParts[0]));
    }

    // ---- UPDATE ----

    [Fact]
    public void Parses_update_with_every_clause()
    {
        const string sql = "update top (10) t with (rowlock) set a += 1, b -= 2, c *= 3, d = default "
            + "output deleted.a, inserted.a from t join u on t.id = u.id where u.x = 1 option (maxdop 1)";
        var update = SqlAssert.ParseTsql<TsqlUpdateStatement>(sql);
        Assert.NotNull(update.Top);
        Assert.IsType<TsqlHintedTable>(update.Target);
        Assert.Equal(Four, update.Assignments.Count);
        Assert.Equal("+=", Text(sql, Assert.NotNull(update.Assignments[0].Operator)));
        Assert.Equal("*=", Text(sql, Assert.NotNull(update.Assignments[Two].Operator)));
        Assert.NotNull(update.Assignments[Three].DefaultKeyword);
        Assert.Single(update.Output);
        Assert.IsType<TableReference>(update.From);
        Assert.Single(update.Joins);
        Assert.NotNull(update.Where);
        Assert.NotNull(update.Option);
        Assert.Equal(sql, SpanText(sql, update.Span));
    }

    [Fact]
    public void Parses_combined_variable_and_column_assignment()
    {
        const string sql = "update t set @v = a = a + 1";
        var clause = Assert.Single(SqlAssert.ParseTsql<TsqlUpdateStatement>(sql).Assignments);
        Assert.Equal("@v", Text(sql, Assert.NotNull(clause.Variable)));
        Assert.Equal("a", SpanText(sql, Ref(clause.Column).Span));
        Assert.Equal("a + 1", SpanText(sql, Ref(clause.Value).Span));
    }

    [Fact]
    public void Parses_variable_assignments_in_update()
    {
        const string sql = "update t set @v = 1, @w += a = b";
        var clauses = SqlAssert.ParseTsql<TsqlUpdateStatement>(sql).Assignments;
        Assert.Null(clauses[0].Column);
        Assert.Equal("1", SpanText(sql, Ref(clauses[0].Value).Span));
        Assert.Equal(SyntaxKind.CompoundAssignmentToken, Assert.NotNull(clauses[1].VariableOperator).Kind);
        Assert.IsType<BinaryExpression>(clauses[1].Value);
    }

    [Fact]
    public void Parses_qualified_set_targets_with_update_from()
    {
        const string sql = "update t set t.a = u.b from dbo.t t inner join u on t.id = u.id";
        var update = SqlAssert.ParseTsql<TsqlUpdateStatement>(sql);
        Assert.IsType<MemberAccessExpression>(update.Assignments[0].Column);
        Assert.Equal("t", Text(sql, Assert.NotNull(Assert.IsType<TableReference>(update.From).Alias)));
    }

    [Theory]
    [InlineData("update t set doc.write(N'x', 0, 1) where id = 1")]
    [InlineData("update t set geo.SetXY(1, 2)")]
    public void Parses_mutator_calls_in_set(string sql)
    {
        var clause = Assert.Single(SqlAssert.ParseTsql<TsqlUpdateStatement>(sql).Assignments);
        Assert.IsType<MethodInvocationExpression>(clause.Column);
        Assert.Null(clause.Operator);
    }

    [Theory]
    [InlineData("update t set a = 1 where current of c", false, "c")]
    [InlineData("update t set a = 1 where current of global c", true, "c")]
    [InlineData("update t set a = 1 where current of @c", false, "@c")]
    public void Parses_where_current_of(string sql, bool global, string cursor)
    {
        var currentOf = Assert.IsType<TsqlCurrentOfClause>(SqlAssert.ParseTsql<TsqlUpdateStatement>(sql).CurrentOf);
        Assert.Equal(global, currentOf.GlobalKeyword is not null);
        Assert.Equal(cursor, Text(sql, currentOf.Cursor));
    }

    [Fact]
    public void Update_statistics_is_not_an_update()
    {
        Assert.NotEqual(typeof(TsqlUpdateStatement), Sql.Parse("update statistics t", SqlOptions.SqlServer).Root?.GetType());
    }

    [Fact]
    public void Compound_set_is_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("update t set a += 1");
        SqlAssert.ParseFails("update t set a = 1 from u");
    }

    // ---- DELETE ----

    [Fact]
    public void Parses_delete_without_from()
    {
        var delete = SqlAssert.ParseTsql<TsqlDeleteStatement>("delete t where a = 1");
        Assert.Null(delete.FromKeyword);
        Assert.NotNull(delete.Where);
    }

    [Fact]
    public void Parses_delete_top_output()
    {
        var delete = SqlAssert.ParseTsql<TsqlDeleteStatement>("delete top (10) from t output deleted.* where a = 1");
        Assert.NotNull(delete.Top);
        Assert.IsType<QualifiedStarExpression>(Assert.Single(delete.Output).Columns[0].Expression);
    }

    [Theory]
    [InlineData("delete t from t join u on t.id = u.id")]
    [InlineData("delete from t from t join u on t.id = u.id where u.x = 1")]
    public void Parses_delete_with_a_second_from(string sql)
    {
        var delete = SqlAssert.ParseTsql<TsqlDeleteStatement>(sql);
        Assert.NotNull(delete.SourceFromKeyword);
        Assert.Single(delete.Joins);
    }

    [Fact]
    public void Parses_delete_with_hints_where_current_of()
    {
        var delete = SqlAssert.ParseTsql<TsqlDeleteStatement>("delete from t with (rowlock) where current of c");
        Assert.IsType<TsqlHintedTable>(delete.Target);
        Assert.NotNull(delete.CurrentOf);
    }

    [Fact]
    public void Delete_without_from_is_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("delete t where a = 1");
    }

    // ---- MERGE ----

    [Fact]
    public void Parses_merge_with_every_when_form_and_output()
    {
        var merge = SqlAssert.ParseTsql<TsqlMergeStatement>(MergeAllWhens);
        var target = Assert.IsType<TsqlHintedTable>(merge.Target);
        Assert.Equal("tgt", Text(MergeAllWhens, Assert.NotNull(Assert.IsType<TableReference>(target.Table).Alias)));
        Assert.IsType<DerivedTable>(merge.Source);
        Assert.Equal(Three, merge.Whens.Count);
        Assert.IsType<TsqlMergeUpdateAction>(merge.Whens[0].Action);
        Assert.NotNull(merge.Whens[0].Condition);
        Assert.Equal("target", Text(MergeAllWhens, Assert.NotNull(merge.Whens[1].ByKind)));
        Assert.IsType<MergeInsertAction>(merge.Whens[1].Action);
        Assert.Equal("source", Text(MergeAllWhens, Assert.NotNull(merge.Whens[Two].ByKind)));
        Assert.IsType<MergeDeleteAction>(merge.Whens[Two].Action);
        var output = Assert.Single(merge.Output);
        Assert.Equal("$action", SpanText(MergeAllWhens, output.Columns[0].Expression.Span));
        Assert.NotNull(merge.Option);
    }

    [Fact]
    public void Parses_merge_top_without_into_and_insert_default_values()
    {
        var merge = SqlAssert.ParseTsql<TsqlMergeStatement>(MergeDefaultValues);
        Assert.Null(merge.IntoKeyword);
        Assert.NotNull(merge.Top);
        Assert.IsType<TsqlMergeInsertDefaultAction>(Assert.Single(merge.Whens).Action);
    }

    [Fact]
    public void Merge_still_needs_its_terminator()
    {
        SqlAssert.ParseFails("merge t using s on t.id = s.id when matched then delete", SqlOptions.SqlServer);
        SqlAssert.ParseFails("with c as (select 1 as a) merge t using c on t.a = c.a when matched then delete", SqlOptions.SqlServer);
    }

    [Fact]
    public void Merge_extensions_are_rejected_without_sql_server()
    {
        SqlAssert.ParseFails("merge t using s on t.id = s.id when matched then delete");
        SqlAssert.ParseFails("merge into t using s on t.id = s.id when matched then delete output $action");
    }

    // ---- WITH before DML ----

    [Fact]
    public void Parses_ctes_before_dml()
    {
        Assert.IsType<TsqlInsertStatement>(SqlAssert.ParseTsql<WithQuery>("with c as (select 1 as a) insert into t select a from c").Query);
        Assert.IsType<TsqlUpdateStatement>(SqlAssert.ParseTsql<WithQuery>("with c as (select 1 as a) update t set b = 1 from t join c on t.a = c.a").Query);
        Assert.IsType<TsqlDeleteStatement>(SqlAssert.ParseTsql<WithQuery>("with c as (select 1 as a) delete from t where a in (select a from c)").Query);
        Assert.IsType<TsqlMergeStatement>(SqlAssert.ParseTsql<WithQuery>(WithMerge).Query);
    }

    // ---- BULK INSERT ----

    [Fact]
    public void Parses_bulk_insert_with_options()
    {
        const string sql = "bulk insert dbo.t from 'c:\\data.csv' with (format = 'CSV', fieldterminator = ',', firstrow = 2, tablock, order (a asc, b desc))";
        var bulk = SqlAssert.ParseTsql<TsqlBulkInsertStatement>(sql);
        Assert.Equal(["dbo", "t"], Texts(sql, bulk.TableName));
        Assert.Equal("'c:\\data.csv'", Text(sql, bulk.Source));
        Assert.Equal(Five, bulk.Options.Count);
        Assert.Equal("format", Text(sql, bulk.Options[0].Name));
        Assert.Null(bulk.Options[Three].EqualsToken);
        Assert.Equal(Two, bulk.Options[Four].Order.Count);
        Assert.Equal(sql, SpanText(sql, bulk.Span));
    }

    [Fact]
    public void Parses_bulk_insert_without_options()
    {
        Assert.Empty(SqlAssert.ParseTsql<TsqlBulkInsertStatement>("bulk insert t from 'x'").Options);
        SqlAssert.ParseFails("bulk insert t from 'x'");
    }

    // ---- TRUNCATE ----

    [Fact]
    public void Parses_truncate_with_partitions()
    {
        const string sql = "truncate table db.dbo.t with (partitions (1, 3 to 5, 7))";
        var truncate = SqlAssert.ParseTsql<TruncateStatement>(sql);
        Assert.Equal(["db", "dbo", "t"], Texts(sql, truncate.TableName));
        var partitions = Assert.IsType<TsqlTruncateExtras>(truncate.SqlServer).Partitions;
        Assert.Equal(Three, partitions.Count);
        Assert.Equal("3 to 5", SpanText(sql, partitions[1].Span));
        Assert.Null(truncate.Postgres);
        Assert.Equal(sql, SpanText(sql, truncate.Span));
    }

    [Fact]
    public void Plain_truncate_has_no_extras()
    {
        Assert.Null(SqlAssert.ParseTsql<TruncateStatement>("truncate table t").SqlServer);
        SqlAssert.ParseFails("truncate table t with (partitions (1))");
    }
}
