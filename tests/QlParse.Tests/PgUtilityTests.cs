namespace QlParse.Tests;

// Workstream F: Postgres utility, session and security statements.
public sealed class PgUtilityTests
{
    private const int TwoItems = 2;
    private const int ThreeItems = 3;
    private const int FourItems = 4;
    private const int AlterTablePrefixLength = 14;
    private const string BareBegin = "begin";
    private const string BareEnd = "end";
    private const string SearchPath = "search_path";
    private const string BareVacuum = "vacuum";
    private const string Checkpoint = "checkpoint";
    private const string Tables = "tables";

    private const string BeginScript = "begin; select 1; end"
        ;

    private const string PsmBlock = "begin set x = 1; end"
        ;

    private static T Whole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParsePg<T>(sql);
        Assert.Equal(new SourceSpan(0, sql.Length), node.Span);
        return node;
    }

    private static Query ParseAny(string sql)
    {
        var result = Sql.Parse(sql, SqlOptions.Postgres);
        Assert.Null(result.Error);
        return Assert.IsAssignableFrom<Query>(result.Root);
    }

    private static string Text(string sql, SyntaxToken? token) =>
        token is SyntaxToken value ? value.TextOf(sql).ToString() : string.Empty;

    // ---- Transactions -------------------------------------------------------------------------

    [Fact]
    public void Parses_bare_begin_as_transaction()
    {
        var begin = Whole<BeginTransactionStatement>(BareBegin);
        Assert.Null(begin.TransactionKeyword);
        Assert.Empty(begin.Modes);
        SqlAssert.ParseFails(BareBegin);
    }

    [Fact]
    public void Parses_begin_work_and_transaction_with_modes()
    {
        const string sql = "begin transaction isolation level serializable, read only, not deferrable";
        var begin = Whole<BeginTransactionStatement>(sql);
        Assert.Equal("transaction", Text(sql, begin.TransactionKeyword));
        Assert.Equal(ThreeItems, begin.Modes.Count);
        Assert.NotNull(begin.Modes[^1].NotKeyword);
        Assert.NotNull(begin.Modes[^1].DeferrableKeyword);
        Assert.NotNull(Whole<BeginTransactionStatement>("begin work").TransactionKeyword);
        Assert.Equal(TwoItems, Whole<BeginTransactionStatement>("begin read write deferrable").Modes.Count);
        Assert.Single(Whole<BeginTransactionStatement>("begin isolation level read committed").Modes);
    }

    [Fact]
    public void Begin_script_is_transaction_then_commit()
    {
        var script = SqlAssert.ParsePg<DirectSqlScript>(BeginScript);
        Assert.IsType<BeginTransactionStatement>(script.Statements[0]);
        Assert.IsType<CommitStatement>(script.Statements[^1]);
    }

    [Fact]
    public void Begin_followed_by_statement_stays_psm_block()
    {
        Assert.IsType<CompoundStatement>(SqlAssert.ParsePg<CompoundStatement>(PsmBlock));
        Assert.IsType<CompoundStatement>(SqlAssert.Parse<CompoundStatement>(PsmBlock));
    }

    [Fact]
    public void Parses_start_transaction_deferrable()
    {
        var start = Whole<StartTransactionStatement>("start transaction read only deferrable");
        Assert.Equal(TwoItems, start.Modes.Count);
        Assert.NotNull(start.Modes[1].DeferrableKeyword);
        SqlAssert.ParseFails("start transaction deferrable");
    }

    [Fact]
    public void Parses_end_and_abort_aliases()
    {
        var end = Whole<CommitStatement>("end transaction");
        Assert.Equal(SyntaxKind.EndKeyword, end.CommitKeyword.Kind);
        Assert.NotNull(end.WorkKeyword);
        Assert.NotNull(Whole<CommitStatement>("end work and chain").ChainKeyword);
        Assert.Null(Whole<CommitStatement>(BareEnd).WorkKeyword);
        var abort = Whole<RollbackStatement>("abort and no chain");
        Assert.NotNull(abort.NoKeyword);
        Assert.NotNull(Whole<RollbackStatement>("abort work").WorkKeyword);
        SqlAssert.ParseFails(BareEnd);
        SqlAssert.ParseFails("abort");
    }

    [Fact]
    public void Parses_commit_and_rollback_chain_and_transaction()
    {
        Assert.NotNull(Whole<CommitStatement>("commit and chain").ChainKeyword);
        Assert.NotNull(Whole<RollbackStatement>("rollback and no chain").NoKeyword);
        Assert.NotNull(Whole<CommitStatement>("commit transaction").WorkKeyword);
        Assert.NotNull(Whole<RollbackStatement>("rollback transaction").WorkKeyword);
        SqlAssert.ParseFails("commit transaction");
    }

    [Fact]
    public void Parses_savepoint_noise_word_as_optional()
    {
        var rollback = Whole<RollbackStatement>("rollback to sp1");
        Assert.Null(rollback.SavepointKeyword);
        Assert.NotNull(rollback.Savepoint);
        Assert.Null(Whole<ReleaseSavepointStatement>("release sp1").SavepointKeyword);
        Assert.NotNull(Whole<ReleaseSavepointStatement>("release savepoint sp1").SavepointKeyword);
        SqlAssert.ParseFails("release sp1");
    }

    [Fact]
    public void Parses_two_phase_commit()
    {
        const string sql = "prepare transaction 'gid1'";
        var prepare = Whole<PrepareTransactionStatement>(sql);
        Assert.Equal("'gid1'", Text(sql, prepare.TransactionId));
        Whole<CommitPreparedStatement>("commit prepared 'gid1'");
        Whole<RollbackPreparedStatement>("rollback prepared 'gid1'");
        SqlAssert.ParseFails(sql);
        SqlAssert.ParseFails("commit prepared 'gid1'");
    }

    [Fact]
    public void Parses_prepare_as_execute_deallocate()
    {
        var prepare = Whole<PrepareAsStatement>("prepare q (int, text) as select * from t where a = 1");
        Assert.Equal(TwoItems, prepare.ParameterTypes.Count);
        Assert.IsType<SelectStatement>(prepare.Statement);
        Assert.IsType<InsertStatement>(Whole<PrepareAsStatement>("prepare q as insert into t values (1)").Statement);
        var execute = Whole<ExecutePreparedStatement>("execute q (1, 'x')");
        Assert.Equal(TwoItems, execute.Arguments.Count);
        Assert.Empty(Whole<ExecutePreparedStatement>("execute q").Arguments);
        Assert.NotNull(Whole<DeallocatePreparedStatement>("deallocate all").AllKeyword);
        Assert.NotNull(Whole<DeallocatePreparedStatement>("deallocate prepare q").PrepareKeyword);
        Assert.NotNull(Whole<DeallocatePreparedStatement>("deallocate q").Name);
        SqlAssert.ParseFails("prepare q as select 1");
        SqlAssert.ParseFails("execute q (1)");
        SqlAssert.ParseFails("deallocate all");
    }

    [Fact]
    public void Iso_prepare_execute_still_parse_under_postgres()
    {
        SqlAssert.ParsePg<PrepareStatement>("prepare s from 'select 1'");
        SqlAssert.ParsePg<ExecuteStatement>("execute s using a");
    }

    [Fact]
    public void Parses_lock_table_modes()
    {
        const string sql = "lock table only t1, s.t2 * in share row exclusive mode nowait";
        var lockTable = Whole<LockTableStatement>(sql);
        Assert.Equal(TwoItems, lockTable.Tables.Count);
        Assert.NotNull(lockTable.Tables[0].OnlyKeyword);
        Assert.NotNull(lockTable.Tables[1].Star);
        Assert.Equal(ThreeItems, lockTable.LockMode.Count);
        Assert.NotNull(lockTable.NowaitKeyword);
        Assert.Empty(Whole<LockTableStatement>("lock t").LockMode);
        SqlAssert.ParseFails("lock table t");
    }

    [Theory]
    [InlineData("access share", 2)]
    [InlineData("row share", 2)]
    [InlineData("row exclusive", 2)]
    [InlineData("share update exclusive", 3)]
    [InlineData("share", 1)]
    [InlineData("share row exclusive", 3)]
    [InlineData("exclusive", 1)]
    [InlineData("access exclusive", 2)]
    public void Parses_every_lock_mode(string mode, int words)
    {
        var lockTable = Whole<LockTableStatement>($"lock t in {mode} mode");
        Assert.Equal(words, lockTable.LockMode.Count);
    }

    // ---- Session configuration ----------------------------------------------------------------

    [Fact]
    public void Parses_set_parameter()
    {
        const string sql = "set search_path = public, 'x'";
        var set = Whole<SetConfigurationStatement>(sql);
        Assert.Equal(SearchPath, Text(sql, set.Name[0]));
        Assert.Equal(SyntaxKind.EqualsToken, set.AssignmentToken.Kind);
        Assert.Equal(TwoItems, set.Values.Count);
        Assert.IsType<SetAssignmentStatement>(SqlAssert.Parse<SetAssignmentStatement>("set search_path = public"));
    }

    [Fact]
    public void Parses_set_local_dotted_default_and_signed_values()
    {
        const string sql = "set local myext.setting to default";
        var set = Whole<SetConfigurationStatement>(sql);
        Assert.Equal("local", Text(sql, set.ScopeKeyword));
        Assert.Equal(TwoItems, set.Name.Count);
        Assert.NotNull(set.DefaultKeyword);
        Assert.Empty(set.Values);
        var signed = Whole<SetConfigurationStatement>("set session extra_float_digits = -3");
        Assert.NotNull(signed.Values[0].Sign);
        Assert.Equal(SyntaxKind.OnKeyword, Whole<SetConfigurationStatement>("set enable_seqscan to on").Values[0].Value.Kind);
        Whole<SetConfigurationStatement>("set statement_timeout = '5s'");
        SqlAssert.ParseFails("set local myext.setting to default");
    }

    [Fact]
    public void Parses_set_time_zone_forms()
    {
        Assert.NotNull(Whole<SetTimeZoneStatement>("set time zone 'UTC'").Value);
        Assert.NotNull(Whole<SetTimeZoneStatement>("set time zone local").LocalKeyword);
        Assert.NotNull(Whole<SetTimeZoneStatement>("set time zone default").DefaultKeyword);
        Assert.NotNull(Whole<SetTimeZoneStatement>("set local time zone 'Europe/Warsaw'").ScopeKeyword);
        SqlAssert.ParseFails("set local time zone 'UTC'");
    }

    [Fact]
    public void Parses_role_authorization_schema_and_constraints_sets()
    {
        Whole<SetRoleStatement>("set role admin");
        Assert.NotNull(Whole<SetRoleStatement>("set local role none").ScopeKeyword);
        Whole<SetSessionAuthorizationStatement>("set session authorization default");
        Whole<SetSchemaStatement>("set schema 'x'");
        Assert.NotNull(Whole<SetConstraintsStatement>("set constraints all deferred").AllKeyword);
        Assert.NotNull(Whole<SetConstraintsStatement>("set constraints c1, c2 immediate").Names);
        Whole<SetTransactionStatement>("set transaction isolation level repeatable read");
        Assert.NotNull(Whole<SetTransactionStatement>("set transaction read write not deferrable").Modes[1].DeferrableKeyword);
    }

    [Fact]
    public void Parses_reset_role_and_session_authorization()
    {
        const string sql = "reset role";
        Assert.Equal("role", Text(sql, Whole<ResetStatement>(sql).Name[0]));
        Assert.Equal(TwoItems, Whole<ResetStatement>("reset session authorization").Name.Count);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Psm_assignment_still_parses_in_iso_mode()
    {
        var block = SqlAssert.Parse<CompoundStatement>(PsmBlock);
        Assert.IsType<SetAssignmentStatement>(block.Statements[0]);
        SqlAssert.Parse<SetAssignmentStatement>("set x = 1 + 2");
    }

    [Fact]
    public void Parses_show()
    {
        const string sql = "show search_path";
        Assert.Equal(SearchPath, Text(sql, Whole<ShowStatement>(sql).Name[0]));
        Assert.NotNull(Whole<ShowStatement>("show all").AllKeyword);
        Assert.Equal(TwoItems, Whole<ShowStatement>("show time zone").Name.Count);
        Assert.Equal(ThreeItems, Whole<ShowStatement>("show transaction isolation level").Name.Count);
        SqlAssert.ParseFails(sql);
        SqlAssert.ParseFails("show all");
    }

    [Fact]
    public void Parses_reset()
    {
        Assert.Single(Whole<ResetStatement>("reset work_mem").Name);
        Assert.NotNull(Whole<ResetStatement>("reset all").AllKeyword);
        Assert.Equal(TwoItems, Whole<ResetStatement>("reset time zone").Name.Count);
        SqlAssert.ParseFails("reset all");
    }

    // ---- Administration -----------------------------------------------------------------------

    [Fact]
    public void Parses_explain_legacy_options()
    {
        var explain = Whole<ExplainStatement>("explain analyze verbose select * from t");
        Assert.Equal(TwoItems, explain.Options.Count);
        Assert.Null(explain.OpenParen);
        Assert.IsType<SelectStatement>(explain.Statement);
        Assert.Empty(Whole<ExplainStatement>("explain select 1").Options);
        Assert.IsType<UpdateStatement>(Whole<ExplainStatement>("explain update t set a = 1").Statement);
        SqlAssert.ParseFails("explain select 1");
    }

    [Fact]
    public void Parses_explain_option_list()
    {
        const string sql = "explain (analyze, buffers off, costs true, format json) select 1";
        var explain = Whole<ExplainStatement>(sql);
        Assert.NotNull(explain.OpenParen);
        Assert.Equal(FourItems, explain.Options.Count);
        Assert.Null(explain.Options[0].Value);
        Assert.Equal("json", Text(sql, explain.Options[3].Value?.Value));
        Assert.IsType<ParenQuery>(Whole<ExplainStatement>("explain (select 1)").Statement);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("xml")]
    [InlineData("json")]
    [InlineData("yaml")]
    public void Parses_explain_formats(string format)
    {
        var explain = Whole<ExplainStatement>($"explain (format {format}) select 1");
        Assert.Single(explain.Options);
        Assert.NotNull(explain.Options[0].Value);
    }

    [Fact]
    public void Parses_vacuum()
    {
        const string sql = "vacuum full freeze verbose analyze t1, s.t2 (a, b)";
        var vacuum = Whole<VacuumStatement>(sql);
        Assert.Equal(FourItems, vacuum.Options.Count);
        Assert.Equal(TwoItems, vacuum.Tables.Count);
        Assert.Equal(TwoItems, vacuum.Tables[1].Columns.Count);
        var listed = Whole<VacuumStatement>("vacuum (full, parallel 4, index_cleanup off) t");
        Assert.Equal(ThreeItems, listed.Options.Count);
        Assert.Empty(Whole<VacuumStatement>(BareVacuum).Tables);
        SqlAssert.ParseFails(BareVacuum);
    }

    [Fact]
    public void Parses_analyze()
    {
        var analyze = Whole<AnalyzeStatement>("analyze verbose t (a)");
        Assert.Single(analyze.Options);
        Assert.Single(analyze.Tables);
        Whole<AnalyzeStatement>("analyse");
        Assert.NotNull(Whole<AnalyzeStatement>("analyze (verbose, skip_locked) a, b").OpenParen);
        SqlAssert.ParseFails("analyze t");
    }

    [Fact]
    public void Parses_cluster()
    {
        var cluster = Whole<ClusterStatement>("cluster verbose s.t using t_idx");
        Assert.Single(cluster.Options);
        Assert.NotNull(cluster.Index);
        Whole<ClusterStatement>("cluster");
        Assert.Null(Whole<ClusterStatement>("cluster (verbose) t").UsingKeyword);
        SqlAssert.ParseFails("cluster t using i");
    }

    [Fact]
    public void Parses_checkpoint()
    {
        Whole<CheckpointStatement>(Checkpoint);
        SqlAssert.ParseFails(Checkpoint);
    }

    [Fact]
    public void Parses_listen_notify_unlisten()
    {
        const string sql = "notify jobs, 'payload'";
        var notify = Whole<NotifyStatement>(sql);
        Assert.Equal("'payload'", Text(sql, notify.Payload));
        Assert.Null(Whole<NotifyStatement>("notify jobs").Payload);
        Whole<ListenStatement>("listen jobs");
        Assert.Equal(SyntaxKind.Star, Whole<UnlistenStatement>("unlisten *").Channel.Kind);
        Whole<UnlistenStatement>("unlisten jobs");
        SqlAssert.ParseFails("listen jobs");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_load()
    {
        Whole<LoadStatement>("load 'auto_explain'");
        SqlAssert.ParseFails("load 'auto_explain'");
    }

    [Theory]
    [InlineData("discard all")]
    [InlineData("discard plans")]
    [InlineData("discard sequences")]
    [InlineData("discard temp")]
    [InlineData("discard temporary")]
    public void Parses_discard(string sql)
    {
        Whole<DiscardStatement>(sql);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_system()
    {
        var alter = Whole<AlterSystemStatement>("alter system set wal_level = replica");
        Assert.IsType<SetConfigurationStatement>(alter.Action);
        Assert.IsType<ResetStatement>(Whole<AlterSystemStatement>("alter system reset all").Action);
        Whole<AlterSystemStatement>("alter system reset wal_level");
        SqlAssert.ParseFails("alter system set wal_level = replica");
    }

    // ---- Roles and privileges -----------------------------------------------------------------

    [Fact]
    public void Parses_create_role_with_options()
    {
        const string sql = "create role r with login password 'x' connection limit -1 valid until '2030-01-01' nosuperuser";
        var role = Whole<CreateRoleStatement>(sql);
        Assert.NotNull(role.WithKeyword);
        Assert.Equal(5, role.Options.Count);
        Assert.Equal(TwoItems, role.Options[TwoItems].Keywords.Count);
        Assert.NotNull(role.Options[TwoItems].Value?.Sign);
        Assert.Equal("nosuperuser", Text(sql, role.Options[4].Keywords[0]));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_create_user_and_group_options()
    {
        var user = Whole<CreateRoleStatement>("create user u encrypted password 'p' in role a, b createdb nocreaterole inherit replication bypassrls");
        Assert.Equal(SyntaxKind.UserKeyword, user.RoleKeyword.Kind);
        Assert.Equal(TwoItems, user.Options[1].Roles.Count);
        Assert.Equal(SyntaxKind.GroupKeyword, Whole<CreateRoleStatement>("create group g role a admin b sysid 5").RoleKeyword.Kind);
        Whole<CreateRoleStatement>("create role r password null");
        Assert.Empty(Whole<CreateRoleStatement>("create role r").Options);
        Assert.Null(SqlAssert.Parse<CreateRoleStatement>("create role r with admin u").Options.FirstOrDefault());
    }

    [Fact]
    public void Parses_alter_role()
    {
        Assert.Equal(TwoItems, Whole<AlterRoleStatement>("alter role r with nologin connection limit 3").Options.Count);
        Assert.NotNull(Whole<AlterRoleStatement>("alter user r rename to s").NewName);
        var set = Whole<AlterRoleStatement>("alter role r in database d set work_mem = '64MB'");
        Assert.NotNull(set.Database);
        Assert.IsType<SetConfigurationStatement>(set.Setting);
        Assert.IsType<ResetStatement>(Whole<AlterRoleStatement>("alter role all reset all").Setting);
        SqlAssert.ParseFails("alter role r with login");
    }

    [Fact]
    public void Parses_drop_role_list()
    {
        var drop = Whole<DropRoleStatement>("drop role if exists a, b");
        Assert.NotNull(drop.IfKeyword);
        Assert.Equal(TwoItems, drop.Names.Count);
        Whole<DropRoleStatement>("drop user u");
        Whole<DropRoleStatement>("drop group g");
        SqlAssert.ParseFails("drop role if exists a, b");
    }

    [Fact]
    public void Parses_grant_on_all_tables_in_schema()
    {
        const string sql = "grant select on all tables in schema s1, s2 to r";
        var grant = Whole<GrantPrivilegeStatement>(sql);
        Assert.NotNull(grant.Object.AllKeyword);
        Assert.Equal(Tables, Text(sql, grant.Object.Kind));
        Assert.Equal(TwoItems, grant.Object.Targets.Count);
        Assert.Equal(new SourceSpan(16, 27), grant.Object.Span);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("grant usage on all sequences in schema s to r")]
    [InlineData("grant execute on all functions in schema s to r")]
    [InlineData("grant execute on all procedures in schema s to r")]
    [InlineData("grant execute on all routines in schema s to r")]
    [InlineData("grant usage, create on schema a, b to r")]
    [InlineData("grant connect, temporary, temp on database d to r")]
    [InlineData("grant usage on sequence s1, s2 to r")]
    [InlineData("grant execute on function f(int, text), g() to r")]
    [InlineData("grant usage on language plpgsql to r")]
    [InlineData("grant create on tablespace ts to r")]
    [InlineData("grant usage on foreign data wrapper w to r")]
    [InlineData("grant usage on foreign server s to r")]
    [InlineData("grant select on large object 1234 to r")]
    [InlineData("grant set on parameter work_mem to r")]
    [InlineData("grant truncate, maintain on t1, t2 to r")]
    [InlineData("grant all on table t to r with grant option")]
    [InlineData("grant all privileges (a, b) on t to r granted by s")]
    [InlineData("grant select (a), update (b) on t to public")]
    [InlineData("grant usage on type ty to r")]
    [InlineData("grant usage on domain d to r")]
    public void Parses_postgres_grant_objects_and_privileges(string sql)
    {
        Whole<GrantPrivilegeStatement>(sql);
    }

    [Fact]
    public void Parses_grant_function_signature()
    {
        var grant = Whole<GrantPrivilegeStatement>("grant execute on function f(int, text) to r");
        Assert.Equal(TwoItems, grant.Object.Targets[0].ArgumentTypes.Count);
        SqlAssert.ParseFails("grant execute on function f(int) to r");
        SqlAssert.ParseFails("grant connect on database d to r");
    }

    [Theory]
    [InlineData("revoke all on all tables in schema s from r")]
    [InlineData("revoke grant option for select on t from r cascade")]
    [InlineData("revoke usage on schema s from public")]
    [InlineData("revoke a from b")]
    [InlineData("revoke admin option for a from b")]
    [InlineData("revoke inherit option for a from b granted by c")]
    [InlineData("revoke set option for a from b restrict")]
    public void Parses_postgres_revoke(string sql)
    {
        var revoke = ParseAny(sql);
        Assert.Equal(new SourceSpan(0, sql.Length), revoke.Span);
    }

    [Fact]
    public void Revoke_behavior_is_optional_only_under_postgres()
    {
        Assert.Null(Whole<RevokePrivilegeStatement>("revoke select on t from r").Behavior);
        SqlAssert.ParseFails("revoke select on t from r");
    }

    [Fact]
    public void Parses_grant_role_options()
    {
        const string sql = "grant a, b to c with inherit true granted by d";
        var grant = Whole<GrantRoleStatement>(sql);
        Assert.Equal("inherit", Text(sql, grant.AdminOption!.Kind));
        Assert.Equal(SyntaxKind.TrueKeyword, grant.AdminOption.OptionKeyword.Kind);
        Assert.NotNull(Whole<GrantRoleStatement>("grant a to b with admin option").AdminOption);
        Whole<GrantRoleStatement>("grant a to b with set false");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_reassign_and_drop_owned()
    {
        var reassign = Whole<ReassignOwnedStatement>("reassign owned by a, b to c");
        Assert.Equal(TwoItems, reassign.Roles.Count);
        Assert.NotNull(Whole<DropOwnedStatement>("drop owned by a cascade").Behavior);
        Whole<DropOwnedStatement>("drop owned by current_user");
        SqlAssert.ParseFails("reassign owned by a to b");
        SqlAssert.ParseFails("drop owned by a");
    }

    [Fact]
    public void Parses_alter_default_privileges()
    {
        const string sql = "alter default privileges for role a in schema s grant select on tables to r";
        var alter = Whole<AlterDefaultPrivilegesStatement>(sql);
        Assert.Single(alter.TargetRoles);
        Assert.Single(alter.Schemas);
        var grant = Assert.IsType<GrantPrivilegeStatement>(alter.Action);
        Assert.Equal(Tables, Text(sql, grant.Object.Kind));
        Assert.IsType<RevokePrivilegeStatement>(
            Whole<AlterDefaultPrivilegesStatement>("alter default privileges revoke execute on functions from public").Action);
        SqlAssert.ParseFails(sql);
    }

    // ---- Row-level security -------------------------------------------------------------------

    [Fact]
    public void Parses_create_policy()
    {
        const string sql = "create policy p on s.t as restrictive for update to a, current_user using (owner = current_user) with check (x > 0)";
        var policy = Whole<CreatePolicyStatement>(sql);
        Assert.Equal("restrictive", Text(sql, policy.PolicyType));
        Assert.Equal(SyntaxKind.UpdateKeyword, policy.Command?.Kind);
        Assert.Equal(TwoItems, policy.Roles.Count);
        Assert.IsType<BinaryExpression>(policy.Using!.Expression);
        Assert.Equal(TwoItems, policy.WithCheck!.Keywords.Count);
        Whole<CreatePolicyStatement>("create policy p on t");
        Whole<CreatePolicyStatement>("create policy p on t for all using (true)");
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_alter_and_drop_policy()
    {
        Assert.NotNull(Whole<AlterPolicyStatement>("alter policy p on t rename to q").NewName);
        var alter = Whole<AlterPolicyStatement>("alter policy p on t to public using (a) with check (b)");
        Assert.NotNull(alter.Using);
        Assert.NotNull(alter.WithCheck);
        var drop = Whole<DropPolicyStatement>("drop policy if exists p on t cascade");
        Assert.NotNull(drop.IfKeyword);
        Assert.NotNull(drop.Behavior);
        Whole<DropPolicyStatement>("drop policy p on t");
        SqlAssert.ParseFails("drop policy p on t");
    }

    [Theory]
    [InlineData("enable", false)]
    [InlineData("disable", false)]
    [InlineData("force", false)]
    [InlineData("no force", true)]
    public void Parses_row_level_security_actions(string verb, bool hasNo)
    {
        var sql = $"alter table t {verb} row level security";
        var alter = Whole<AlterTableStatement>(sql);
        var action = Assert.IsType<RowLevelSecurityAction>(alter.Action);
        Assert.Equal(hasNo, action.NoKeyword is not null);
        Assert.Equal(new SourceSpan(AlterTablePrefixLength, sql.Length - AlterTablePrefixLength), action.Span);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_security_label()
    {
        const string sql = "security label for selinux on table s.t is 'system_u:object_r:sepgsql_table_t:s0'";
        var label = Whole<SecurityLabelStatement>(sql);
        Assert.Equal("selinux", Text(sql, label.Provider));
        Assert.Equal(TwoItems, label.Object.Name.Count);
        Assert.Equal(TwoItems, Whole<SecurityLabelStatement>("security label on materialized view v is null").ObjectKind.Count);
        Whole<SecurityLabelStatement>("security label on function f(int) is 'x'");
        SqlAssert.ParseFails(sql);
    }

    // ---- COMMENT ON and databases -------------------------------------------------------------

    [Theory]
    [InlineData("comment on table t is 'x'")]
    [InlineData("comment on column t.c is 'x'")]
    [InlineData("comment on index i is null")]
    [InlineData("comment on schema s is 'x'")]
    [InlineData("comment on type ty is 'x'")]
    [InlineData("comment on view v is 'x'")]
    [InlineData("comment on extension e is 'x'")]
    [InlineData("comment on role r is 'x'")]
    [InlineData("comment on database d is 'x'")]
    [InlineData("comment on materialized view m is 'x'")]
    [InlineData("comment on foreign data wrapper w is 'x'")]
    public void Parses_postgres_comment_kinds(string sql)
    {
        Whole<CommentStatement>(sql);
    }

    [Fact]
    public void Parses_comment_with_signature_and_table_scope()
    {
        const string sql = "comment on function s.f(int, text) is 'f'";
        var function = Whole<CommentStatement>(sql);
        Assert.Equal(TwoItems, function.ArgumentTypes.Count);
        var trigger = Whole<CommentStatement>("comment on trigger tr on s.t is 'x'");
        Assert.NotNull(trigger.TargetOnKeyword);
        Assert.Equal(TwoItems, trigger.TableName!.Count);
        Whole<CommentStatement>("comment on policy p on t is 'x'");
        Whole<CommentStatement>("comment on constraint c on t is 'x'");
        Whole<CommentStatement>("comment on constraint c on domain d is 'x'");
        SqlAssert.ParseFails(sql);
        SqlAssert.ParseFails("comment on trigger tr on t is 'x'");
    }

    [Fact]
    public void Parses_create_database()
    {
        const string sql = "create database d with owner = bob template template0 encoding 'UTF8' locale 'C' lc_collate 'C' "
            + "lc_ctype 'C' tablespace ts connection limit 10 is_template false";
        var create = Whole<CreateDatabaseStatement>(sql);
        Assert.Equal(9, create.Options.Count);
        Assert.NotNull(create.Options[0].EqualsToken);
        Assert.Equal(TwoItems, create.Options[7].Keywords.Count);
        Assert.Empty(Whole<CreateDatabaseStatement>("create database d").Options);
        SqlAssert.ParseFails("create database d");
    }

    [Fact]
    public void Parses_alter_database()
    {
        Assert.Equal(TwoItems, Whole<AlterDatabaseStatement>("alter database d rename to e").ActionKeywords.Count);
        Assert.NotNull(Whole<AlterDatabaseStatement>("alter database d owner to current_user").Target);
        Whole<AlterDatabaseStatement>("alter database d set tablespace ts");
        Assert.IsType<SetConfigurationStatement>(Whole<AlterDatabaseStatement>("alter database d set work_mem to '1MB'").Setting);
        Assert.IsType<ResetStatement>(Whole<AlterDatabaseStatement>("alter database d reset all").Setting);
        Assert.Single(Whole<AlterDatabaseStatement>("alter database d with connection limit 5").Options);
        SqlAssert.ParseFails("alter database d rename to e");
    }

    [Fact]
    public void Parses_drop_database()
    {
        var drop = Whole<DropDatabaseStatement>("drop database if exists d with (force)");
        Assert.NotNull(drop.IfKeyword);
        Assert.Single(drop.Options);
        Whole<DropDatabaseStatement>("drop database d");
        SqlAssert.ParseFails("drop database d");
    }

    // ---- Visitor ------------------------------------------------------------------------------

    [Theory]
    [InlineData("begin isolation level serializable")]
    [InlineData("prepare transaction 'g'")]
    [InlineData("commit prepared 'g'")]
    [InlineData("rollback prepared 'g'")]
    [InlineData("set a = 1")]
    [InlineData("show a")]
    [InlineData("reset a")]
    [InlineData("alter system set a = 1")]
    [InlineData("prepare q (int) as select 1")]
    [InlineData("execute q (1)")]
    [InlineData("deallocate q")]
    [InlineData("lock t")]
    [InlineData("explain select 1")]
    [InlineData("vacuum t")]
    [InlineData("analyze t")]
    [InlineData("cluster t")]
    [InlineData(Checkpoint)]
    [InlineData("listen c")]
    [InlineData("notify c")]
    [InlineData("unlisten c")]
    [InlineData("load 'x'")]
    [InlineData("discard all")]
    [InlineData("alter role r set a = 1")]
    [InlineData("reassign owned by a to b")]
    [InlineData("drop owned by a")]
    [InlineData("alter default privileges grant select on tables to r")]
    [InlineData("create policy p on t using (a = 1) with check (b)")]
    [InlineData("alter policy p on t using (a)")]
    [InlineData("drop policy p on t")]
    [InlineData("security label on function f(int) is 'x'")]
    [InlineData("create database d")]
    [InlineData("alter database d reset all")]
    [InlineData("drop database d")]
    [InlineData("alter table t enable row level security")]
    public void Default_visitor_walks_utility_statements(string sql)
    {
        new SqlVisitor().Visit(ParseAny(sql));
    }
}
