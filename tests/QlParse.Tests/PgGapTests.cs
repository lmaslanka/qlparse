namespace QlParse.Tests;

// Postgres gaps closed across the workstreams: query/DML (C) and utility/security (F) forms here,
// DDL (D) and schema-object (E) forms in PgGapTests.Schema.cs.
public sealed partial class PgGapTests
{
    private const string CascadeText = "cascade";
    private const string RoleText = "r";
    private const string TransactionText = "transaction";

    private static void AssertSpan(string sql, SourceSpan span, string expected) =>
        Assert.Equal(expected, sql[span.Position..span.End]);

    private static void AssertWhole(string sql, Query query) =>
        Assert.Equal(new SourceSpan(0, sql.Length), query.Span);

    private static T WalkPg<T>(string sql) where T : Query
    {
        var query = SqlAssert.ParsePg<T>(sql);
        AssertWhole(sql, query);
        new SqlVisitor().Visit(query);
        return query;
    }

    // ---- 1. TRUNCATE with several tables, identity and drop behavior ----

    [Fact]
    public void Parses_truncate_of_several_tables_with_options()
    {
        const string sql = "truncate table only a, b * restart identity cascade";
        var truncate = WalkPg<TruncateStatement>(sql);
        var extras = Assert.IsType<PgTruncateExtras>(truncate.Postgres);
        Assert.NotNull(extras.OnlyKeyword);
        var second = Assert.Single(extras.AdditionalTables);
        AssertSpan(sql, second.Span, "b *");
        Assert.NotNull(second.Star);
        AssertSpan(sql, Assert.NotNull(truncate.IdentityKeyword).Span, "identity");
        AssertSpan(sql, Assert.NotNull(extras.Behavior).Span, CascadeText);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("truncate a, b")]
    [InlineData("truncate a continue identity restrict")]
    [InlineData("truncate only a, only s.b, c")]
    public void Parses_truncate_lists_and_behaviors(string sql)
    {
        Assert.NotNull(WalkPg<TruncateStatement>(sql).Postgres);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Plain_truncate_has_no_postgres_extras()
    {
        Assert.Null(SqlAssert.ParsePg<TruncateStatement>("truncate table t").Postgres);
    }

    // ---- 2. LATERAL before a function or ROWS FROM ----

    [Fact]
    public void Parses_lateral_function_in_comma_from()
    {
        const string sql = "select * from t, lateral generate_series(1, t.n) g";
        var select = SqlAssert.SelectPg(sql);
        var lateral = Assert.IsType<LateralTable>(Assert.Single(select.ExtraFrom).Table);
        AssertSpan(sql, lateral.Span, "lateral generate_series(1, t.n) g");
        var function = Assert.IsType<TableFunction>(lateral.Table);
        AssertSpan(sql, Assert.NotNull(function.Alias).Span, "g");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_cross_join_lateral_function()
    {
        const string sql = "select * from t cross join lateral f(t.x)";
        var join = Assert.Single(WalkPg<SelectStatement>(sql).Joins);
        var lateral = Assert.IsType<LateralTable>(join.Table);
        AssertSpan(sql, lateral.Span, "lateral f(t.x)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_lateral_rows_from()
    {
        const string sql = "select * from t left join lateral rows from (f(1), g(2)) as x(a, b) on true";
        var lateral = Assert.IsType<LateralTable>(Assert.Single(WalkPg<SelectStatement>(sql).Joins).Table);
        var rowsFrom = Assert.IsType<RowsFromTable>(lateral.Table);
        Assert.Equal(Count.Two, rowsFrom.Functions.Count);
        AssertSpan(sql, lateral.Span, "lateral rows from (f(1), g(2)) as x(a, b)");
    }

    [Fact]
    public void Lateral_subquery_stays_a_derived_table()
    {
        const string sql = "select * from t join lateral (select 1) s on true";
        var derived = Assert.IsType<DerivedTable>(Assert.Single(SqlAssert.SelectPg(sql).Joins).Table);
        AssertSpan(sql, Assert.NotNull(derived.LateralKeyword).Span, "lateral");
    }

    // ---- 3 / 6. ORDER BY, LIMIT and locking after TABLE, VALUES and set operations ----

    [Fact]
    public void Parses_table_with_order_by_and_limit()
    {
        const string sql = "table t order by a limit 3";
        var ordered = WalkPg<OrderedQuery>(sql);
        var table = Assert.IsType<ExplicitTableQuery>(ordered.Query);
        AssertSpan(sql, table.Span, "table t");
        AssertSpan(sql, ordered.OrderBy!.Span, "order by a");
        AssertSpan(sql, ordered.Limit!.Span, "limit 3");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Ordering_after_set_operation_of_tables_applies_to_the_whole_operation()
    {
        const string sql = "table t union table u order by 1";
        var ordered = WalkPg<OrderedQuery>(sql);
        var union = Assert.IsType<SetOperation>(ordered.Query);
        Assert.IsType<ExplicitTableQuery>(union.Right);
        AssertSpan(sql, union.Span, "table t union table u");
        AssertSpan(sql, ordered.OrderBy!.Span, "order by 1");
    }

    [Fact]
    public void Parses_values_with_order_by_and_limit()
    {
        const string sql = "values (1), (2) order by 1 limit 1";
        var ordered = WalkPg<OrderedQuery>(sql);
        Assert.Equal(Count.Two, Assert.IsType<ValuesQuery>(ordered.Query).Rows.Count);
        AssertSpan(sql, ordered.Limit!.Span, "limit 1");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_ordered_table_inside_a_cte_body_and_a_lock_after_table()
    {
        const string sql = "with c as (table t order by a) table c for update";
        var with = WalkPg<WithQuery>(sql);
        var cteBody = Assert.IsType<OrderedQuery>(with.Ctes[0].Query);
        AssertSpan(sql, cteBody.Span, "table t order by a");
        var outer = Assert.IsType<OrderedQuery>(with.Query);
        AssertSpan(sql, outer.Lock!.Span, "for update");
    }

    [Fact]
    public void Trailing_select_keeps_its_own_order_by_after_a_set_operation()
    {
        const string sql = "select a from t union select b from u order by 1";
        var union = SqlAssert.ParsePg<SetOperation>(sql);
        AssertSpan(sql, Assert.IsType<SelectStatement>(union.Right).OrderBy!.Span, "order by 1");
    }

    [Fact]
    public void Parses_for_update_after_union_and_cte()
    {
        const string unionSql = "select a from t union select b from u for update";
        var union = WalkPg<SetOperation>(unionSql);
        AssertSpan(unionSql, Assert.IsType<SelectStatement>(union.Right).Lock!.Span, "for update");

        const string cteSql = "with c as (select 1) select * from c for update";
        var with = WalkPg<WithQuery>(cteSql);
        AssertSpan(cteSql, Assert.IsType<SelectStatement>(with.Query).Lock!.Span, "for update");
    }

    // ---- 4. (TABLE t) as an expression subquery ----

    [Fact]
    public void Parses_in_table_subquery()
    {
        const string sql = "select * from u where a in (table t)";
        var where = Assert.IsType<WhereClause>(WalkPg<SelectStatement>(sql).Where);
        var inExpression = Assert.IsType<InExpression>(where.Expression);
        var table = Assert.IsType<ExplicitTableQuery>(inExpression.Query);
        AssertSpan(sql, table.Span, "table t");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_scalar_table_subquery()
    {
        const string sql = "select (table only t)";
        var subquery = SqlAssert.ExprPg<ScalarSubqueryExpression>("(table only t)");
        Assert.NotNull(Assert.IsType<ExplicitTableQuery>(subquery.Query).OnlyKeyword);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_exists_table_subquery()
    {
        const string sql = "select exists (table t)";
        var exists = Assert.IsType<ExistsExpression>(WalkPg<SelectStatement>(sql).SelectList[0].Expression);
        AssertSpan(sql, exists.Query.Span, "table t");
    }

    // ---- 5. qualified names in a lock's OF list ----

    [Fact]
    public void Parses_qualified_lock_targets()
    {
        const string sql = "select * from s.t, u for update of s.t, u";
        var lockClause = Assert.IsType<LockClause>(WalkPg<SelectStatement>(sql).Lock);
        Assert.Equal(Count.Two, lockClause.Columns!.Count);
        AssertSpan(sql, lockClause.Columns[0].Span, "t");
        var names = lockClause.Postgres!.OfNames;
        Assert.Equal(Count.Two, names[0].Count);
        AssertSpan(sql, lockClause.Span, "for update of s.t, u");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Unqualified_lock_targets_keep_no_postgres_names()
    {
        var lockClause = SqlAssert.SelectPg("select * from t for update of t").Lock!;
        Assert.Null(lockClause.Postgres);
        Assert.Single(lockClause.Columns!);
    }

    // ---- 7. MERGE Postgres forms ----

    [Fact]
    public void Parses_merge_do_nothing()
    {
        const string sql = "merge into t using s on t.id = s.id when matched then do nothing when not matched then do nothing";
        var merge = WalkPg<MergeStatement>(sql);
        Assert.Equal(Count.Two, merge.Whens.Count);
        var matched = Assert.IsType<MergeDoNothingAction>(merge.Whens[0].Action);
        AssertSpan(sql, matched.Span, "do nothing");
        Assert.IsType<MergeDoNothingAction>(merge.Whens[1].Action);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("when not matched by source then delete")]
    [InlineData("when not matched by target then insert values (s.id)")]
    [InlineData("when matched and t.x > 1 then delete")]
    [InlineData("when not matched by source and t.x > 1 then do nothing")]
    public void Parses_merge_by_source_target_and_conditional_delete(string whenText)
    {
        var sql = $"merge into t using s on t.id = s.id {whenText}";
        var when = Assert.Single(WalkPg<MergeStatement>(sql).Whens);
        AssertSpan(sql, when.Span, whenText);
    }

    // ---- 8. INSERT ... SELECT with ON CONFLICT and a CTE, DEFAULT VALUES RETURNING ----

    [Fact]
    public void Parses_cte_insert_select_on_conflict()
    {
        const string sql = "with c as (select 1 a) insert into t select a from c on conflict (a) do nothing";
        var insert = Assert.IsType<InsertStatement>(WalkPg<WithQuery>(sql).Query);
        Assert.IsType<SelectStatement>(insert.Query);
        AssertSpan(sql, insert.Postgres!.OnConflict!.Span, "on conflict (a) do nothing");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_insert_default_values_returning()
    {
        const string sql = "insert into t default values returning *";
        var insert = WalkPg<InsertStatement>(sql);
        Assert.NotNull(insert.DefaultKeyword);
        AssertSpan(sql, insert.Postgres!.Returning!.Span, "returning *");
        SqlAssert.ParseFails(sql);
    }

    // ---- 9. GRANT role WITH several options ----

    [Fact]
    public void Parses_role_grant_with_several_options()
    {
        const string sql = "grant r to u with admin option, inherit true, set false";
        var grant = WalkPg<GrantRoleStatement>(sql);
        AssertSpan(sql, grant.AdminOption!.Span, "with admin option");
        Assert.Equal(Count.Two, grant.AdditionalOptions.Count);
        AssertSpan(sql, grant.AdditionalOptions[0].Span, "inherit true");
        AssertSpan(sql, grant.AdditionalOptions[1].Value.Span, "false");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Single_role_grant_option_has_no_additional_options()
    {
        Assert.Empty(SqlAssert.ParsePg<GrantRoleStatement>("grant r to u with admin option").AdditionalOptions);
    }

    // ---- 10. ALTER GROUP ADD / DROP USER ----

    [Theory]
    [InlineData("alter group g add user a, b", "add user a, b")]
    [InlineData("alter group g drop user a", "drop user a")]
    public void Parses_alter_group_members(string sql, string member)
    {
        var alter = WalkPg<AlterRoleStatement>(sql);
        Assert.Equal(SyntaxKind.GroupKeyword, alter.RoleKeyword.Kind);
        var option = Assert.Single(alter.Options);
        AssertSpan(sql, option.Span, member);
        Assert.Equal(Count.Two, option.Keywords.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_group_rename()
    {
        const string sql = "alter group g rename to h";
        AssertSpan(sql, Assert.NotNull(WalkPg<AlterRoleStatement>(sql).NewName).Span, "h");
    }

    // ---- 11. SET TRANSACTION SNAPSHOT, SET SESSION CHARACTERISTICS AS TRANSACTION ----

    [Fact]
    public void Parses_set_transaction_snapshot()
    {
        const string sql = "set transaction snapshot '000003A1-1'";
        var set = WalkPg<SetTransactionStatement>(sql);
        Assert.Empty(set.Modes);
        AssertSpan(sql, Assert.NotNull(set.SnapshotId).Span, "'000003A1-1'");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_session_characteristics_as_transaction_under_postgres()
    {
        const string sql = "set session characteristics as transaction read only";
        var set = WalkPg<SetSessionCharacteristicsStatement>(sql);
        AssertSpan(sql, Assert.NotNull(set.TransactionKeyword).Span, TransactionText);
        AssertSpan(sql, Assert.Single(set.Modes).Span, "read only");
    }

    [Fact]
    public void Parses_session_characteristics_as_transaction_under_iso()
    {
        const string sql = "set session characteristics as transaction isolation level serializable, read only";
        var set = SqlAssert.Parse<SetSessionCharacteristicsStatement>(sql);
        AssertWhole(sql, set);
        AssertSpan(sql, Assert.NotNull(set.TransactionKeyword).Span, TransactionText);
        Assert.Equal(Count.Two, set.Modes.Count);
    }

    // ---- 12. SET ... FROM CURRENT in ALTER ROLE / ALTER DATABASE ----

    [Fact]
    public void Parses_alter_role_set_from_current()
    {
        const string sql = "alter role r in database d set search_path from current";
        var alter = WalkPg<AlterRoleStatement>(sql);
        var setting = Assert.IsType<SetConfigurationStatement>(alter.Setting);
        AssertSpan(sql, setting.Span, "set search_path from current");
        Assert.Equal(SyntaxKind.FromKeyword, setting.AssignmentToken.Kind);
        AssertSpan(sql, Assert.NotNull(setting.CurrentKeyword).Span, "current");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_database_set_from_current()
    {
        const string sql = "alter database d set search_path from current";
        var setting = Assert.IsType<SetConfigurationStatement>(WalkPg<AlterDatabaseStatement>(sql).Setting);
        Assert.Empty(setting.Values);
        SqlAssert.ParseFails(sql);
    }

    // ---- 13. CLUSTER index ON table, CLUSTER (VERBOSE) table ----

    [Fact]
    public void Parses_old_cluster_index_on_table()
    {
        const string sql = "cluster idx on s.t";
        var cluster = WalkPg<ClusterStatement>(sql);
        AssertSpan(sql, Assert.NotNull(cluster.Index).Span, "idx");
        Assert.NotNull(cluster.OnKeyword);
        Assert.Equal(Count.Two, cluster.Table!.Count);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("cluster (verbose) t")]
    [InlineData("cluster verbose t using idx")]
    public void Parses_cluster_with_options(string sql)
    {
        var cluster = WalkPg<ClusterStatement>(sql);
        Assert.Null(cluster.OnKeyword);
        Assert.Single(cluster.Table!);
    }

    // ---- 14. ALTER DEFAULT PRIVILEGES with FOR / IN in either order ----

    [Theory]
    [InlineData("alter default privileges in schema s for role r grant select on tables to u")]
    [InlineData("alter default privileges for role r in schema s grant select on tables to u")]
    public void Parses_default_privileges_clauses_in_either_order(string sql)
    {
        var alter = WalkPg<AlterDefaultPrivilegesStatement>(sql);
        AssertSpan(sql, Assert.Single(alter.TargetRoles).Span, RoleText);
        AssertSpan(sql, Assert.Single(alter.Schemas)[0].Span, "s");
        Assert.IsType<GrantPrivilegeStatement>(alter.Action);
        SqlAssert.ParseFails(sql);
    }

    // ---- 15. Postgres SET takes only configuration values ----

    [Fact]
    public void Postgres_set_rejects_an_expression_value()
    {
        // Postgres itself accepts only configuration values (names, strings, numbers) after SET x =.
        SqlAssert.ParseFails("set x = 1 + 2", SqlOptions.Postgres);
        Assert.IsType<SetConfigurationStatement>(SqlAssert.ParsePg<SetConfigurationStatement>("set x = 3"));
    }
}
