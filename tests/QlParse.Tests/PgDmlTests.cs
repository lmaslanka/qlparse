namespace QlParse.Tests;

// Postgres query-expression and DML syntax (workstream C), parsed under SqlOptions.Postgres.
public sealed class PgDmlTests
{
    private static void AssertSpan(string sql, SourceSpan span, string expected) =>
        Assert.Equal(expected, sql[span.Position..span.End]);

    private static void AssertWhole(string sql, Query query) =>
        Assert.Equal(new SourceSpan(0, sql.Length), query.Span);

    // ---- DISTINCT ON ----

    [Fact]
    public void Parses_distinct_on()
    {
        const string sql = "select distinct on (a, b) a, b from t order by a";
        var select = SqlAssert.SelectPg(sql);
        Assert.NotNull(select.DistinctKeyword);
        var distinctOn = Assert.IsType<DistinctOnClause>(select.Postgres?.DistinctOn);
        Assert.Equal(Count.Two, distinctOn.Expressions.Count);
        Assert.Equal(new SourceSpan(16, 9), distinctOn.Span);
        Assert.Equal(Count.Two, select.SelectList.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Plain_distinct_is_unchanged_under_postgres()
    {
        var select = SqlAssert.SelectPg("select distinct a from t");
        Assert.NotNull(select.DistinctKeyword);
        Assert.Null(select.Postgres);
    }

    // ---- LIMIT ALL, OFFSET ROWS ----

    [Fact]
    public void Parses_limit_all()
    {
        const string sql = "select a from t limit all";
        var limit = Assert.IsType<LimitClause>(SqlAssert.SelectPg(sql).Limit);
        Assert.NotNull(limit.Postgres?.AllKeyword);
        Assert.Null(limit.Count);
        AssertSpan(sql, limit.Span, "limit all");
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("select a from t limit 10 offset 2 rows")]
    [InlineData("select a from t offset 2 rows limit 10")]
    [InlineData("select a from t limit all offset 2 row")]
    public void Parses_limit_and_offset_rows_in_either_order(string sql)
    {
        var select = SqlAssert.SelectPg(sql);
        Assert.NotNull(select.Limit);
        Assert.NotNull(Assert.IsType<OffsetClause>(select.Offset).RowKeyword);
        AssertWhole(sql, select);
    }

    [Fact]
    public void Parses_fetch_with_ties()
    {
        const string sql = "select a from t order by a fetch first 2 rows with ties";
        var fetch = Assert.IsType<FetchClause>(SqlAssert.SelectPg(sql).Fetch);
        Assert.NotNull(fetch.TiesKeyword);
        AssertSpan(sql, fetch.Span, "fetch first 2 rows with ties");
    }

    // ---- WITH ... [NOT] MATERIALIZED, SEARCH, CYCLE ----

    [Fact]
    public void Parses_materialized_cte()
    {
        const string sql = "with x as materialized (select 1) select * from x";
        var cte = SqlAssert.ParsePg<WithQuery>(sql).Ctes[0];
        Assert.Null(cte.Postgres!.NotKeyword);
        AssertSpan(sql, cte.Postgres.MaterializedKeyword.Span, "materialized");
        AssertSpan(sql, cte.Span, "x as materialized (select 1)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_not_materialized_cte()
    {
        var cte = SqlAssert.ParsePg<WithQuery>("with x as not materialized (select 1) select * from x").Ctes[0];
        var materialization = Assert.IsType<PgCteExtras>(cte.Postgres);
        Assert.NotNull(materialization.NotKeyword);
        Assert.IsType<SelectStatement>(cte.Query);
    }

    [Fact]
    public void Parses_search_and_iso_cycle_under_postgres()
    {
        const string sql = "with recursive x(id) as (select 1) search depth first by id set ord cycle id set mark to true default false using path select * from x";
        var cte = SqlAssert.ParsePg<WithQuery>(sql).Ctes[0];
        Assert.NotNull(cte.Search);
        Assert.NotNull(Assert.IsType<CycleClause>(cte.Cycle).MarkValue);
    }

    [Fact]
    public void Parses_pg14_cycle_without_mark_values()
    {
        const string sql = "with recursive x(id) as (select 1) search breadth first by id set ord cycle id set is_cycle using path select * from x";
        var cycle = Assert.IsType<CycleClause>(SqlAssert.ParsePg<WithQuery>(sql).Ctes[0].Cycle);
        Assert.Null(cycle.ToKeyword);
        Assert.Null(cycle.MarkValue);
        Assert.Null(cycle.DefaultValue);
        AssertSpan(sql, cycle.Span, "cycle id set is_cycle using path");
        SqlAssert.ParseFails(sql);
    }

    // ---- TABLE name ----

    [Fact]
    public void Parses_explicit_table()
    {
        const string sql = "table s.t";
        var table = SqlAssert.ParsePg<ExplicitTableQuery>(sql);
        Assert.Equal(Count.Two, table.NameParts.Count);
        AssertWhole(sql, table);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_explicit_table_with_only_and_star()
    {
        Assert.NotNull(SqlAssert.ParsePg<ExplicitTableQuery>("table only t").OnlyKeyword);
        Assert.NotNull(SqlAssert.ParsePg<ExplicitTableQuery>("table t *").DescendantStar);
    }

    [Fact]
    public void Parses_explicit_table_as_set_operand()
    {
        var union = SqlAssert.ParsePg<SetOperation>("table a union all table b");
        Assert.IsType<ExplicitTableQuery>(union.Left);
        Assert.IsType<ExplicitTableQuery>(union.Right);
        Assert.IsType<ExplicitTableQuery>(SqlAssert.ParsePg<SetOperation>("select 1 except table b").Right);
    }

    [Fact]
    public void Parses_explicit_table_in_cte_and_derived_table()
    {
        var with = SqlAssert.ParsePg<WithQuery>("with x as (table t) table x");
        Assert.IsType<ExplicitTableQuery>(with.Ctes[0].Query);
        Assert.IsType<ExplicitTableQuery>(with.Query);
        var derived = Assert.IsType<DerivedTable>(SqlAssert.SelectPg("select * from (table t) as s").From);
        Assert.IsType<ExplicitTableQuery>(derived.Query);
    }

    // ---- WITH ORDINALITY and column definition lists ----

    [Fact]
    public void Parses_generic_function_with_ordinality()
    {
        const string sql = "select * from generate_series(1, 3) with ordinality as g(v, n)";
        var function = Assert.IsType<TableFunction>(SqlAssert.SelectPg(sql).From);
        var ordinality = Assert.IsType<PgTableFunctionExtras>(function.Postgres);
        AssertSpan(sql, SourceSpan.From(ordinality.WithKeyword!.Value, ordinality.OrdinalityKeyword!.Value), "with ordinality");
        Assert.Equal(Count.Two, Assert.IsType<IReadOnlyList<SyntaxToken>>(function.Columns, exactMatch: false).Count);
        AssertSpan(sql, function.Span, "generate_series(1, 3) with ordinality as g(v, n)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_unnest_with_ordinality_under_postgres()
    {
        var unnest = Assert.IsType<UnnestTable>(SqlAssert.SelectPg("select * from unnest(a) with ordinality as u(x, n)").From);
        Assert.NotNull(unnest.OrdinalityKeyword);
    }

    [Fact]
    public void Parses_column_definition_list()
    {
        const string sql = "select * from json_to_record(j) as t(a int, b text)";
        var function = Assert.IsType<TableFunction>(SqlAssert.SelectPg(sql).From);
        Assert.Null(function.Columns);
        var definitions = function.Postgres!.ColumnDefinitions;
        Assert.Equal(Count.Two, definitions.Count);
        AssertSpan(sql, definitions[1].Span, "b text");
        AssertSpan(sql, function.Span, "json_to_record(j) as t(a int, b text)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_column_definition_list_without_alias()
    {
        var function = Assert.IsType<TableFunction>(SqlAssert.SelectPg("select * from f(x) as (a int, b varchar(10)) where a > 1").From);
        Assert.Null(function.Alias);
        Assert.Equal(Count.Two, function.Postgres!.ColumnDefinitions.Count);
    }

    // ---- ROWS FROM ----

    [Fact]
    public void Parses_rows_from()
    {
        const string sql = "select * from rows from (f(1), g(2)) as t";
        var rows = Assert.IsType<RowsFromTable>(SqlAssert.SelectPg(sql).From);
        Assert.Equal(Count.Two, rows.Functions.Count);
        Assert.IsType<FunctionCallExpression>(rows.Functions[0].Function);
        AssertSpan(sql, rows.Span, "rows from (f(1), g(2)) as t");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_rows_from_with_definitions_and_ordinality()
    {
        const string sql = "select * from rows from (f(1) as (a int), g(2) as (b text, c int)) with ordinality as t(a, b, c, n)";
        var rows = Assert.IsType<RowsFromTable>(SqlAssert.SelectPg(sql).From);
        Assert.Single(rows.Functions[0].ColumnDefinitions);
        Assert.Equal(Count.Two, rows.Functions[1].ColumnDefinitions.Count);
        AssertSpan(sql, rows.Functions[1].Span, "g(2) as (b text, c int)");
        Assert.NotNull(rows.OrdinalityKeyword);
        Assert.Equal(Count.Four, Assert.IsType<IReadOnlyList<SyntaxToken>>(rows.Columns, exactMatch: false).Count);
    }

    [Fact]
    public void Table_named_rows_still_parses()
    {
        Assert.IsType<TableReference>(SqlAssert.SelectPg("select * from rows where a = 1").From);
    }

    // ---- RETURNING ----

    [Fact]
    public void Parses_insert_returning()
    {
        const string sql = "insert into t values (1) returning id";
        var insert = SqlAssert.ParsePg<InsertStatement>(sql);
        var returning = Assert.IsType<ReturningClause>(insert.Postgres?.Returning);
        Assert.Single(returning.Items);
        AssertSpan(sql, returning.Span, "returning id");
        AssertWhole(sql, insert);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Returning_stays_a_bare_alias_in_iso_mode()
    {
        var select = SqlAssert.Select("select a returning from t returning");
        Assert.NotNull(select.SelectList[0].Alias);
        Assert.NotNull(Assert.IsType<TableReference>(select.From).Alias);
    }

    [Fact]
    public void Parses_returning_star_and_aliases()
    {
        var returning = SqlAssert.ParsePg<InsertStatement>("insert into t select a from s returning *, id as x, id + 1 y").Postgres!.Returning!;
        Assert.Equal(Count.Three, returning.Items.Count);
        Assert.IsType<StarExpression>(returning.Items[0].Expression);
        Assert.NotNull(returning.Items[1].AsKeyword);
        Assert.NotNull(returning.Items[Count.Two].Alias);
    }

    [Fact]
    public void Parses_returning_with_old_and_new()
    {
        const string sql = "update t set a = 1 returning with (old as o, new as n) o.a, n.a";
        var returning = SqlAssert.ParsePg<UpdateStatement>(sql).Postgres!.Returning!;
        Assert.Equal(Count.Two, returning.OutputAliases.Count);
        AssertSpan(sql, returning.OutputAliases[1].Span, "new as n");
        Assert.Equal(Count.Two, returning.Items.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_update_delete_and_merge_returning()
    {
        const string merge = "merge into t using s on t.id = s.id when matched then delete returning merge_action(), t.*";
        Assert.NotNull(SqlAssert.ParsePg<UpdateStatement>("update t set a = 1 where b = 2 returning a").Postgres?.Returning);
        Assert.NotNull(SqlAssert.ParsePg<DeleteStatement>("delete from t returning *").Postgres?.Returning);
        var statement = SqlAssert.ParsePg<MergeStatement>(merge);
        Assert.Equal(Count.Two, statement.Postgres!.Returning!.Items.Count);
        AssertWhole(merge, statement);
        SqlAssert.ParseFails(merge);
    }

    // ---- ON CONFLICT ----

    [Fact]
    public void Parses_on_conflict_do_nothing()
    {
        const string sql = "insert into t (a) values (1) on conflict (a) do nothing";
        var conflict = Assert.IsType<OnConflictClause>(SqlAssert.ParsePg<InsertStatement>(sql).Postgres?.OnConflict);
        Assert.NotNull(conflict.NothingKeyword);
        Assert.Single(Assert.IsType<ConflictTarget>(conflict.Target).Elements);
        AssertSpan(sql, conflict.Span, "on conflict (a) do nothing");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_on_conflict_without_target()
    {
        var conflict = SqlAssert.ParsePg<InsertStatement>("insert into t select * from s on conflict do nothing").Postgres!.OnConflict!;
        Assert.Null(conflict.Target);
    }

    [Fact]
    public void Parses_on_conflict_do_update()
    {
        const string sql = "insert into t as x (a, b) values (1, 2) on conflict (a, lower(b) collate en_us text_pattern_ops, (a + 1)) where a > 0 do update set b = excluded.b, (a, c) = (excluded.a, default) where x.b <> excluded.b returning x.*";
        var insert = SqlAssert.ParsePg<InsertStatement>(sql);
        var conflict = insert.Postgres!.OnConflict!;
        var target = conflict.Target!;
        Assert.Equal(Count.Three, target.Elements.Count);
        Assert.IsType<CollateExpression>(target.Elements[1].Expression);
        Assert.Single(target.Elements[1].OperatorClass);
        AssertSpan(sql, target.Elements[1].Span, "lower(b) collate en_us text_pattern_ops");
        Assert.NotNull(target.Where);
        Assert.NotNull(conflict.UpdateKeyword);
        Assert.Equal(Count.Two, conflict.Assignments.Count);
        Assert.Equal(Count.Two, conflict.Assignments[1].Targets.Count);
        Assert.NotNull(conflict.Where);
        Assert.NotNull(insert.Postgres.Returning);
        AssertWhole(sql, insert);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_on_conflict_on_constraint()
    {
        const string sql = "insert into t values (1) on conflict on constraint t_pkey do update set a = default";
        var target = SqlAssert.ParsePg<InsertStatement>(sql).Postgres!.OnConflict!.Target!;
        Assert.NotNull(target.ConstraintKeyword);
        Assert.Empty(target.Elements);
        AssertSpan(sql, target.Span, "on constraint t_pkey");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_excluded_pseudo_table_in_do_update()
    {
        const string sql = "insert into t (a, b) values (1, 2) on conflict (a) do update set b = excluded.b";
        var value = SqlAssert.ParsePg<InsertStatement>(sql).Postgres!.OnConflict!.Assignments[0].Value;
        var member = Assert.IsType<MemberAccessExpression>(value);
        var table = Assert.IsType<IdentifierExpression>(member.Target);
        AssertSpan(sql, table.Span, "excluded");
        AssertSpan(sql, member.Span, "excluded.b");
    }

    // ---- INSERT alias, OVERRIDING, DEFAULT VALUES ----

    [Fact]
    public void Parses_insert_alias()
    {
        const string sql = "insert into s.t as x (a) values (1)";
        var insert = SqlAssert.ParsePg<InsertStatement>(sql);
        Assert.NotNull(insert.Postgres!.AsKeyword);
        AssertSpan(sql, Assert.NotNull(insert.Postgres.Alias).Span, "x");
        Assert.NotNull(insert.Columns);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("insert into t overriding system value values (1)")]
    [InlineData("insert into t (a) overriding user value select 1")]
    public void Parses_overriding_under_postgres(string sql)
    {
        Assert.NotNull(SqlAssert.ParsePg<InsertStatement>(sql).Override);
    }

    [Fact]
    public void Parses_default_values_under_postgres()
    {
        const string sql = "insert into t as x default values returning *";
        var insert = SqlAssert.ParsePg<InsertStatement>(sql);
        Assert.NotNull(insert.DefaultKeyword);
        Assert.NotNull(insert.Postgres?.Returning);
        AssertWhole(sql, insert);
    }

    // ---- UPDATE ... FROM ----

    [Fact]
    public void Parses_update_from()
    {
        const string sql = "update t set a = s.a from s where t.id = s.id";
        var update = SqlAssert.ParsePg<UpdateStatement>(sql);
        Assert.NotNull(update.Postgres!.FromKeyword);
        Assert.IsType<TableReference>(update.Postgres.From);
        Assert.NotNull(update.Where);
        AssertWhole(sql, update);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_update_from_with_joins_alias_and_current_of()
    {
        const string sql = "update t as x set a = 1 from s join u on s.id = u.id, v where current of c";
        var update = SqlAssert.ParsePg<UpdateStatement>(sql);
        Assert.NotNull(update.Target.Alias);
        Assert.Single(update.Postgres!.Joins);
        Assert.Single(update.Postgres.ExtraFrom);
        Assert.NotNull(update.Positioned);
        AssertWhole(sql, update);
    }

    [Fact]
    public void Parses_update_where_current_of_under_postgres()
    {
        Assert.NotNull(SqlAssert.ParsePg<UpdateStatement>("update t set a = 1 where current of c").Positioned);
    }

    // ---- DELETE ... USING ----

    [Fact]
    public void Parses_delete_using()
    {
        const string sql = "delete from t using s, u where t.id = s.id";
        var delete = SqlAssert.ParsePg<DeleteStatement>(sql);
        Assert.NotNull(delete.Postgres!.UsingKeyword);
        Assert.IsType<TableReference>(delete.Postgres.Using);
        Assert.Single(delete.Postgres.ExtraUsing);
        AssertWhole(sql, delete);
        SqlAssert.ParseFails(sql);
    }

    // ---- Writable CTEs ----

    [Fact]
    public void Parses_writable_cte()
    {
        const string sql = "with t as (insert into x values (1) returning id) select * from t";
        var with = SqlAssert.ParsePg<WithQuery>(sql);
        var insert = Assert.IsType<InsertStatement>(with.Ctes[0].Query);
        AssertSpan(sql, insert.Span, "insert into x values (1) returning id");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_chained_writable_ctes_with_dml_main_statement()
    {
        const string sql = "with d as (delete from x returning *), u as (update y set a = 1 returning a) insert into z select * from d";
        var with = SqlAssert.ParsePg<WithQuery>(sql);
        Assert.IsType<DeleteStatement>(with.Ctes[0].Query);
        Assert.IsType<UpdateStatement>(with.Ctes[1].Query);
        Assert.IsType<InsertStatement>(with.Query);
        AssertWhole(sql, with);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("with t as (select 1) update x set a = 1")]
    [InlineData("with t as (select 1) delete from x")]
    [InlineData("with t as (select 1) merge into x using t on true when matched then delete")]
    public void Parses_with_and_dml_main_statement(string sql)
    {
        Assert.IsNotType<SelectStatement>(SqlAssert.ParsePg<WithQuery>(sql).Query);
        SqlAssert.ParseFails(sql);
    }

    // ---- COPY ----

    [Fact]
    public void Parses_copy_from_stdin()
    {
        const string sql = "copy t from stdin";
        var copy = SqlAssert.ParsePg<CopyStatement>(sql);
        Assert.Single(copy.TableName);
        AssertSpan(sql, copy.Target.Span, "stdin");
        Assert.Equal(new SourceSpan(0, 17), copy.Span);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_copy_with_columns_and_option_list()
    {
        const string sql = "copy s.t (a, b) to stdout with (format csv, header, delimiter ',', force_quote (a, b), null '')";
        var copy = SqlAssert.ParsePg<CopyStatement>(sql);
        Assert.Equal(Count.Two, copy.TableName.Count);
        Assert.Equal(Count.Two, copy.Columns!.Count);
        Assert.NotNull(copy.WithKeyword);
        Assert.Equal(Count.Five, copy.Options.Count);
        Assert.Null(copy.Options[1].Value);
        Assert.Equal(Count.Two, copy.Options[Count.Three].Values.Count);
        AssertSpan(sql, copy.Options[Count.Four].Span, "null ''");
        AssertWhole(sql, copy);
    }

    [Fact]
    public void Parses_copy_query_to_file()
    {
        const string sql = "copy (select * from t where a > 1) to '/tmp/x.csv' (format csv, header true)";
        var copy = SqlAssert.ParsePg<CopyStatement>(sql);
        Assert.Empty(copy.TableName);
        Assert.IsType<SelectStatement>(copy.Query);
        Assert.Null(copy.WithKeyword);
        Assert.Equal(Count.Two, copy.Options.Count);
        AssertWhole(sql, copy);
    }

    [Fact]
    public void Parses_copy_dml_query()
    {
        Assert.IsType<DeleteStatement>(SqlAssert.ParsePg<CopyStatement>("copy (delete from t returning *) to stdout").Query);
    }

    [Fact]
    public void Parses_copy_program_with_legacy_options()
    {
        const string sql = "copy t from program 'gunzip -c x.gz' with csv header delimiter as ',' null '' force not null a, b";
        var copy = SqlAssert.ParsePg<CopyStatement>(sql);
        Assert.NotNull(copy.ProgramKeyword);
        Assert.Null(copy.OptionsOpenParen);
        Assert.Equal(Count.Five, copy.Options.Count);
        Assert.NotNull(copy.Options[Count.Two].AsKeyword);
        var force = copy.Options[Count.Four];
        Assert.Equal(Count.Two, force.NameTail.Count);
        Assert.Equal(Count.Two, force.Values.Count);
        AssertSpan(sql, force.Span, "force not null a, b");
        AssertWhole(sql, copy);
    }

    [Theory]
    [InlineData("copy t to stdout csv force quote *")]
    [InlineData("copy t from stdin with (on_error ignore, header match, reject_limit 10, force_null *)")]
    [InlineData("copy t from stdin binary")]
    public void Parses_copy_option_variants(string sql)
    {
        Assert.NotEmpty(SqlAssert.ParsePg<CopyStatement>(sql).Options);
    }

    [Fact]
    public void Parses_copy_where()
    {
        const string sql = "copy t from '/data/t.csv' with (format csv) where a > 1";
        var copy = SqlAssert.ParsePg<CopyStatement>(sql);
        AssertSpan(sql, copy.Where!.Span, "where a > 1");
        AssertWhole(sql, copy);
    }

    [Fact]
    public void Copy_does_not_parse_inline_data_rows()
    {
        // psql streams the rows after FROM STDIN; they are not SQL and are not parsed.
        SqlAssert.ParseFails("copy t from stdin 1 2 3", SqlOptions.Postgres);
    }

    // ---- ONLY and descendant inclusion ----

    [Fact]
    public void Parses_from_only_without_parentheses()
    {
        const string sql = "select * from only s.t as x where a = 1";
        var table = Assert.IsType<OnlyTable>(SqlAssert.SelectPg(sql).From);
        Assert.Null(table.OpenParen);
        Assert.Equal(Count.Two, table.NameParts.Count);
        Assert.NotNull(table.Alias);
        AssertSpan(sql, table.Span, "only s.t as x");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_from_only_with_parentheses_under_postgres()
    {
        Assert.NotNull(Assert.IsType<OnlyTable>(SqlAssert.SelectPg("select * from only (t)").From).OpenParen);
    }

    [Fact]
    public void Parses_descendant_star()
    {
        const string sql = "select * from t * x join u * on true";
        var select = SqlAssert.SelectPg(sql);
        var table = Assert.IsType<DescendantTable>(select.From);
        Assert.NotNull(table.Alias);
        AssertSpan(sql, table.Span, "t * x");
        Assert.IsType<DescendantTable>(select.Joins[0].Table);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_update_and_delete_only()
    {
        const string update = "update only t set a = 1";
        const string delete = "delete from only t * where a = 1";
        var updateTarget = SqlAssert.ParsePg<UpdateStatement>(update).Target;
        Assert.NotNull(updateTarget.OnlyKeyword);
        Assert.Null(updateTarget.OpenParen);
        AssertSpan(update, updateTarget.Span, "only t");
        var deleteStatement = SqlAssert.ParsePg<DeleteStatement>(delete);
        var deleteTarget = deleteStatement.Target;
        Assert.NotNull(deleteStatement.Postgres?.TargetDescendantStar);
        AssertSpan(delete, deleteTarget.Span, "only t *");
        SqlAssert.ParseFails(update);
        SqlAssert.ParseFails(delete);
    }

    [Fact]
    public void Parses_update_star_and_delete_only_parenthesized()
    {
        Assert.NotNull(SqlAssert.ParsePg<UpdateStatement>("update t * set a = 1").Postgres?.TargetDescendantStar);
        Assert.NotNull(SqlAssert.ParsePg<DeleteStatement>("delete from only (t)").Target.OpenParen);
    }

    [Fact]
    public void Parses_truncate_only()
    {
        const string sql = "truncate only t";
        var truncate = SqlAssert.ParsePg<TruncateStatement>(sql);
        Assert.Null(truncate.TableKeyword);
        Assert.NotNull(truncate.Postgres?.OnlyKeyword);
        AssertWhole(sql, truncate);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_truncate_table_star_restart_identity()
    {
        const string sql = "truncate table t * restart identity";
        var truncate = SqlAssert.ParsePg<TruncateStatement>(sql);
        Assert.NotNull(truncate.TableKeyword);
        Assert.NotNull(truncate.Postgres?.DescendantStar);
        Assert.NotNull(truncate.IdentityKeyword);
        AssertWhole(sql, truncate);
    }

    // ---- Locking clauses ----

    [Fact]
    public void Parses_for_no_key_update_of_nowait()
    {
        const string sql = "select a from t for no key update of t nowait";
        var lockClause = Assert.IsType<LockClause>(SqlAssert.SelectPg(sql).Lock);
        Assert.NotNull(lockClause.Postgres!.NoKeyword);
        AssertSpan(sql, SourceSpan.From(lockClause.Postgres.NoKeyword!.Value, lockClause.Postgres.KeyKeyword!.Value), "no key");
        Assert.NotNull(lockClause.UpdateKeyword);
        Assert.NotNull(lockClause.Postgres.NowaitKeyword);
        AssertSpan(sql, lockClause.Span, "for no key update of t nowait");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_for_share_skip_locked()
    {
        const string sql = "select a from t for share skip locked";
        var lockClause = SqlAssert.SelectPg(sql).Lock!;
        Assert.NotNull(lockClause.ShareKeyword);
        Assert.NotNull(lockClause.Postgres?.LockedKeyword);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_multiple_locks_then_limit()
    {
        const string sql = "select a from t, u for key share of t for update of u skip locked limit 1";
        var select = SqlAssert.SelectPg(sql);
        Assert.Null(select.Lock!.Postgres!.NoKeyword);
        Assert.NotNull(select.Lock.Postgres.NextLock!.Postgres?.SkipKeyword);
        Assert.NotNull(select.Limit);
        AssertWhole(sql, select);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("select a from t limit 1 for update")]
    [InlineData("select a from t for read only")]
    [InlineData("select a from t for update")]
    public void Parses_iso_lock_forms_under_postgres(string sql)
    {
        Assert.NotNull(SqlAssert.SelectPg(sql).Lock);
    }

    // ---- SELECT INTO ----

    [Fact]
    public void Parses_select_into_table()
    {
        const string sql = "select a into unlogged table s.new_t from t";
        var select = SqlAssert.SelectPg(sql);
        var into = Assert.IsType<SelectIntoTable>(select.Postgres?.IntoTable);
        Assert.NotNull(into.Modifier);
        Assert.NotNull(into.TableKeyword);
        Assert.Equal(Count.Two, into.NameParts.Count);
        AssertSpan(sql, into.Span, "unlogged table s.new_t");
        Assert.Null(select.IntoTargets);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_plain_select_into_under_postgres()
    {
        var into = SqlAssert.SelectPg("select a into temp new_t from t").Postgres!.IntoTable!;
        Assert.NotNull(into.Modifier);
        Assert.Single(into.NameParts);
    }

    // ---- Visitor ----

    private sealed class PgRecorder : SqlVisitor
    {
        private readonly List<Type> _types = [];

        public IReadOnlyList<Type> Types => _types;

        public override void VisitExplicitTableQuery(ExplicitTableQuery node)
        {
            _types.Add(typeof(ExplicitTableQuery));
            base.VisitExplicitTableQuery(node);
        }

        public override void VisitRowsFromTable(RowsFromTable node)
        {
            _types.Add(typeof(RowsFromTable));
            base.VisitRowsFromTable(node);
        }

        public override void VisitReturningClause(ReturningClause node)
        {
            _types.Add(typeof(ReturningClause));
            base.VisitReturningClause(node);
        }

        public override void VisitConflictTargetElement(ConflictTargetElement node)
        {
            _types.Add(typeof(ConflictTargetElement));
            base.VisitConflictTargetElement(node);
        }

        public override void VisitCopyOption(CopyOption node)
        {
            _types.Add(typeof(CopyOption));
            base.VisitCopyOption(node);
        }

        public override void VisitPgLockExtras(PgLockExtras node)
        {
            _types.Add(typeof(PgLockExtras));
            base.VisitPgLockExtras(node);
        }

        public override void VisitPgTruncateExtras(PgTruncateExtras node)
        {
            _types.Add(typeof(PgTruncateExtras));
            base.VisitPgTruncateExtras(node);
        }

        public override void VisitDistinctOnClause(DistinctOnClause node)
        {
            _types.Add(typeof(DistinctOnClause));
            base.VisitDistinctOnClause(node);
        }

        public override void VisitDataType(DataType node)
        {
            _types.Add(typeof(DataType));
            base.VisitDataType(node);
        }

        public override void VisitIdentifierExpression(IdentifierExpression node)
        {
            _types.Add(typeof(IdentifierExpression));
            base.VisitIdentifierExpression(node);
        }
    }

    [Theory]
    [InlineData("table t", typeof(ExplicitTableQuery))]
    [InlineData("select a from t for update nowait", typeof(PgLockExtras))]
    [InlineData("truncate only t", typeof(PgTruncateExtras))]
    [InlineData("select * from t * where x = 1", typeof(IdentifierExpression))]
    [InlineData("select * from rows from (f(1) as (a int)) with ordinality", typeof(RowsFromTable))]
    [InlineData("select * from f(1) as t(a int)", typeof(DataType))]
    [InlineData("insert into t values (1) on conflict (a) do update set b = excluded.b returning *", typeof(ConflictTargetElement))]
    [InlineData("with d as (delete from t returning *) table d", typeof(ReturningClause))]
    [InlineData("merge into t using s on true when matched then delete returning *", typeof(ReturningClause))]
    [InlineData("copy (select 1) to stdout (format csv)", typeof(CopyOption))]
    [InlineData("select distinct on (a) a from t limit all for update skip locked", typeof(DistinctOnClause))]
    [InlineData("update t set a = 1 from s where s.b = 1 returning a", typeof(IdentifierExpression))]
    [InlineData("delete from t using s where x = 1 returning y", typeof(IdentifierExpression))]
    [InlineData("select a into new_t from t", typeof(IdentifierExpression))]
    [InlineData("with recursive x as (select id from t) cycle id set m using p select * from x", typeof(IdentifierExpression))]
    public void Visitor_walks_postgres_nodes(string sql, Type expected)
    {
        var result = Sql.Parse(sql, SqlOptions.Postgres);
        Assert.Null(result.Error);
        var visitor = new PgRecorder();
        visitor.Visit(result.Root!);
        Assert.Contains(expected, visitor.Types);
    }
}
