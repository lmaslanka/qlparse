namespace QlParse.Tests;

// Workstream F: SQL Server permissions, principals and execution context. Databases, schemas and other objects are
// tested in the Database part, and keys, certificates, credentials, audits and row-level security in the Crypto part.
public sealed partial class TsqlSecurityTests
{
    private const int TwoItems = 2;
    private const int ThreeItems = 3;
    private const string Principal = "u";
    private const string Dbo = "dbo";
    private const string CertificateName = "c";
    private const string CookieVariable = "@c";
    private const string DatabaseName = "d";
    private const string DropWord = "drop";
    private const string FilegroupName = "fg1";
    private const string KeyName = "k";
    private const string LoginWord = "login";
    private const string OnWord = "on";
    private const string PrimaryWord = "primary";
    private const string RevertWord = "revert";
    private const string SchemaName = "s";
    private const string UserWord = "user";

    private static T Whole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParseTsql<T>(sql);
        Assert.Equal(new SourceSpan(0, sql.Length), node.Span);
        return node;
    }

    private static string Text(string sql, SyntaxToken? token) =>
        token is SyntaxToken value ? value.TextOf(sql).ToString() : string.Empty;

    private static string Words(string sql, IReadOnlyList<SyntaxToken> tokens) =>
        string.Join(' ', tokens.Select(token => token.TextOf(sql).ToString()));

    private static string Words(string sql, TsqlObjectOption option) => Words(sql, option.Keywords);

    private static string Value(string sql, TsqlObjectOption option) => Words(sql, option.Value);

    // ---- GRANT / DENY / REVOKE ----------------------------------------------------------------

    [Fact]
    public void Parses_grant_with_columns_grant_option_and_as()
    {
        const string sql = "grant select, insert, update (a, b) on dbo.t to u, [v] with grant option as dbo";
        var grant = Whole<TsqlPermissionStatement>(sql);
        Assert.Equal("grant", Text(sql, grant.VerbKeyword));
        Assert.Equal(["select", "insert", "update"], grant.Permissions.Select(permission => Words(sql, permission.Keywords)));
        Assert.Equal(["a", "b"], grant.Permissions[^1].Columns.Select(column => Text(sql, column)));
        Assert.Empty(grant.Securable!.ClassKeywords);
        Assert.Equal("dbo t", Words(sql, grant.Securable.Name));
        Assert.Equal([Principal, "[v]"], grant.Principals.Select(principal => Text(sql, principal)));
        Assert.NotNull(grant.GrantOption);
        Assert.Equal(Dbo, Text(sql, grant.AsPrincipal));
    }

    [Fact]
    public void Parses_class_qualified_securables()
    {
        const string sql = "grant execute on xml schema collection::dbo.c to u";
        var grant = Whole<TsqlPermissionStatement>(sql);
        Assert.Equal("xml schema collection", Words(sql, grant.Securable!.ClassKeywords));
        Assert.Equal("::", Text(sql, grant.Securable.ColonColon));
        Assert.Equal("dbo c", Words(sql, grant.Securable.Name));
        const string userSql = "grant impersonate on user::u to v";
        Assert.Equal(UserWord, Words(userSql, Whole<TsqlPermissionStatement>(userSql).Securable!.ClassKeywords));
        const string schemaSql = "grant view definition on schema::s to u";
        var schema = Whole<TsqlPermissionStatement>(schemaSql);
        Assert.Equal("view definition", Words(schemaSql, schema.Permissions[0].Keywords));
        Assert.Equal("schema", Words(schemaSql, schema.Securable!.ClassKeywords));
    }

    [Fact]
    public void Parses_securable_column_list_and_multi_word_permissions()
    {
        const string sql = "grant select on dbo.t (a, b) to u";
        var grant = Whole<TsqlPermissionStatement>(sql);
        Assert.Equal(["a", "b"], grant.Columns.Select(column => Text(sql, column)));
        Assert.NotNull(grant.OpenParen);
        const string anySql = "grant alter any user to u";
        var any = Whole<TsqlPermissionStatement>(anySql);
        Assert.Equal("alter any user", Words(anySql, any.Permissions[0].Keywords));
        Assert.Null(any.OnKeyword);
        Assert.Null(any.Securable);
        const string allSql = "grant all privileges on t to u";
        Assert.Equal("all privileges", Words(allSql, Whole<TsqlPermissionStatement>(allSql).Permissions[0].Keywords));
        const string createSql = "grant create table to u";
        Assert.Equal("create table", Words(createSql, Whole<TsqlPermissionStatement>(createSql).Permissions[0].Keywords));
    }

    [Fact]
    public void Parses_deny()
    {
        const string sql = "deny select on object::dbo.t to u cascade";
        var deny = Whole<TsqlPermissionStatement>(sql);
        Assert.Equal("deny", Text(sql, deny.VerbKeyword));
        Assert.Equal("object", Words(sql, deny.Securable!.ClassKeywords));
        Assert.NotNull(deny.CascadeKeyword);
        const string serverSql = "deny view server state to l";
        Assert.Equal("view server state", Words(serverSql, Whole<TsqlPermissionStatement>(serverSql).Permissions[0].Keywords));
    }

    [Fact]
    public void Parses_revoke_forms()
    {
        const string sql = "revoke grant option for select on t to u cascade";
        var revoke = Whole<TsqlPermissionStatement>(sql);
        Assert.Equal("grant option for", Words(sql, revoke.GrantOptionFor));
        Assert.Equal("to", Text(sql, revoke.ToKeyword));
        Assert.NotNull(revoke.CascadeKeyword);
        const string fromSql = "revoke execute on schema::s from u as dbo";
        var from = Whole<TsqlPermissionStatement>(fromSql);
        Assert.Empty(from.GrantOptionFor);
        Assert.Equal("from", Text(fromSql, from.ToKeyword));
        Assert.Equal(Dbo, Text(fromSql, from.AsPrincipal));
    }

    [Fact]
    public void Plain_grant_and_revoke_become_permission_statements_under_sql_server()
    {
        Assert.IsType<TsqlPermissionStatement>(SqlAssert.ParseTsql<TsqlPermissionStatement>("grant select on t to u"));
        Assert.IsType<GrantPrivilegeStatement>(SqlAssert.Parse<GrantPrivilegeStatement>("grant select on t to u"));
        Assert.IsType<RevokePrivilegeStatement>(SqlAssert.Parse<RevokePrivilegeStatement>("revoke select on t from u cascade"));
    }

    [Theory]
    [InlineData("deny select on t to u")]
    [InlineData("grant control on database::d to u")]
    [InlineData("grant create table to u")]
    [InlineData("revoke select on t from u")]
    public void Permission_syntax_is_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Theory]
    [InlineData("grant on t to u")]
    [InlineData("grant select on t")]
    [InlineData("grant select on t from u")]
    [InlineData("deny select on object:: to u")]
    public void Rejects_malformed_permissions(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Fact]
    public void Permission_statement_ends_before_next_statement()
    {
        const string sql = "grant select on t to u select 1";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.IsType<TsqlPermissionStatement>(batch.Statements[0]);
        Assert.IsType<SelectStatement>(batch.Statements[1]);
    }

    // ---- EXECUTE AS / REVERT ------------------------------------------------------------------

    [Fact]
    public void Parses_execute_as_forms()
    {
        const string sql = "execute as user = 'u' with cookie into @c";
        var execute = Whole<TsqlExecuteAsStatement>(sql);
        Assert.Equal(UserWord, Text(sql, execute.ContextKeyword));
        Assert.Equal("'u'", Text(sql, execute.Principal));
        Assert.Equal("cookie into", Words(sql, execute.WithKeywords));
        Assert.Equal(CookieVariable, Text(sql, execute.Cookie));
        const string loginSql = "exec as login = N'l' with no revert";
        var login = Whole<TsqlExecuteAsStatement>(loginSql);
        Assert.Equal(LoginWord, Text(loginSql, login.ContextKeyword));
        Assert.Equal("no revert", Words(loginSql, login.WithKeywords));
        Assert.Null(login.Cookie);
        foreach (var context in new[] { "caller", "self", "owner" })
        {
            var sql2 = $"execute as {context}";
            var bare = Whole<TsqlExecuteAsStatement>(sql2);
            Assert.Equal(context, Text(sql2, bare.ContextKeyword));
            Assert.Null(bare.Principal);
        }
    }

    [Fact]
    public void Parses_revert()
    {
        Assert.Null(Whole<TsqlRevertStatement>(RevertWord).CookieKeyword);
        const string sql = "revert with cookie = @c";
        Assert.Equal(CookieVariable, Text(sql, Whole<TsqlRevertStatement>(sql).Cookie));
    }

    [Theory]
    [InlineData("execute as user")]
    [InlineData("execute as somebody")]
    [InlineData("execute as login = l")]
    public void Rejects_malformed_execute_as(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Theory]
    [InlineData("execute as user = 'u'")]
    [InlineData(RevertWord)]
    public void Execute_as_and_revert_are_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    // ---- Logins, users and roles ------------------------------------------------------------

    [Fact]
    public void Parses_create_login_with_password_options()
    {
        const string sql = "create login l with password = 'p' hashed must_change, check_policy = on, default_database = d";
        var login = Whole<TsqlCreatePrincipalStatement>(sql);
        Assert.Equal(LoginWord, Words(sql, login.KindKeywords));
        Assert.Equal(ThreeItems, login.Options.Count);
        Assert.Equal("password", Words(sql, login.Options[0]));
        Assert.Equal("'p' hashed must_change", Value(sql, login.Options[0]));
        Assert.Equal("on", Value(sql, login.Options[1]));
        const string sidSql = "create login l with password = 0x0100 hashed, sid = 0x241C";
        Assert.Equal("0x0100 hashed", Value(sidSql, Whole<TsqlCreatePrincipalStatement>(sidSql).Options[0]));
    }

    [Fact]
    public void Parses_create_login_sources()
    {
        const string sql = "create login [dom\\u] from windows with default_database = d";
        var windows = Whole<TsqlCreatePrincipalStatement>(sql);
        Assert.Equal("from windows", Words(sql, windows.SourceKeywords));
        Assert.Null(windows.Source);
        Assert.Single(windows.Options);
        const string keySql = "create login l from asymmetric key k";
        var key = Whole<TsqlCreatePrincipalStatement>(keySql);
        Assert.Equal("from asymmetric key", Words(keySql, key.SourceKeywords));
        Assert.Equal("k", Text(keySql, key.Source));
        const string providerSql = "create login l from external provider";
        Assert.Equal("from external provider", Words(providerSql, Whole<TsqlCreatePrincipalStatement>(providerSql).SourceKeywords));
    }

    [Fact]
    public void Parses_create_user_forms()
    {
        const string sql = "create user u for login l with default_schema = s";
        var user = Whole<TsqlCreatePrincipalStatement>(sql);
        Assert.Equal(UserWord, Words(sql, user.KindKeywords));
        Assert.Equal("for login", Words(sql, user.SourceKeywords));
        Assert.Equal("l", Text(sql, user.Source));
        Assert.Equal("default_schema", Words(sql, user.Options[0]));
        const string withoutSql = "create user u without login";
        Assert.Equal("without login", Words(withoutSql, Whole<TsqlCreatePrincipalStatement>(withoutSql).SourceKeywords));
        const string containedSql = "create user u with password = 'p', default_schema = dbo";
        Assert.Equal(TwoItems, Whole<TsqlCreatePrincipalStatement>(containedSql).Options.Count);
        Assert.Equal("c", Text("create user u for certificate c", Whole<TsqlCreatePrincipalStatement>("create user u for certificate c").Source));
    }

    [Fact]
    public void Parses_create_roles()
    {
        const string sql = "create role r authorization dbo";
        var role = Whole<TsqlCreatePrincipalStatement>(sql);
        Assert.Equal("role", Words(sql, role.KindKeywords));
        Assert.Equal(Dbo, Text(sql, role.Owner));
        Assert.Null(Whole<TsqlCreatePrincipalStatement>("create role r").AuthorizationKeyword);
        const string serverSql = "create server role sr authorization sa";
        Assert.Equal("server role", Words(serverSql, Whole<TsqlCreatePrincipalStatement>(serverSql).KindKeywords));
        const string appSql = "create application role ar with password = 'p', default_schema = s";
        var app = Whole<TsqlCreatePrincipalStatement>(appSql);
        Assert.Equal("application role", Words(appSql, app.KindKeywords));
        Assert.Equal(TwoItems, app.Options.Count);
        Assert.IsType<CreateRoleStatement>(SqlAssert.Parse<CreateRoleStatement>("create role r"));
    }

    [Fact]
    public void Parses_alter_role_membership()
    {
        const string sql = "alter role r add member u";
        var alter = Whole<TsqlAlterPrincipalStatement>(sql);
        Assert.Equal("add member", Words(sql, alter.ActionKeywords));
        Assert.Equal(Principal, Text(sql, alter.Member));
        const string dropSql = "alter server role sysadmin drop member l";
        var drop = Whole<TsqlAlterPrincipalStatement>(dropSql);
        Assert.Equal("server role", Words(dropSql, drop.KindKeywords));
        Assert.Equal("drop member", Words(dropSql, drop.ActionKeywords));
        const string renameSql = "alter role r with name = r2";
        Assert.Equal("name", Words(renameSql, Whole<TsqlAlterPrincipalStatement>(renameSql).Options[0]));
    }

    [Fact]
    public void Parses_alter_login_and_user()
    {
        Assert.Equal("enable", Text("alter login l enable", Whole<TsqlAlterPrincipalStatement>("alter login l enable").ActionKeywords[0]));
        const string sql = "alter login l with password = 'new' old_password = 'old'";
        var password = Whole<TsqlAlterPrincipalStatement>(sql);
        Assert.Equal(["password", "old_password"], password.Options.Select(option => Words(sql, option)));
        const string credentialSql = "alter login l add credential c";
        Assert.Equal("add credential", Words(credentialSql, Whole<TsqlAlterPrincipalStatement>(credentialSql).ActionKeywords));
        const string userSql = "alter user u with name = u2, default_schema = s";
        Assert.Equal(TwoItems, Whole<TsqlAlterPrincipalStatement>(userSql).Options.Count);
    }

    [Theory]
    [InlineData("alter role r add u")]
    [InlineData("alter login l")]
    [InlineData("create login l from")]
    [InlineData("create user u with")]
    public void Rejects_malformed_principals(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Theory]
    [InlineData("create login l with password = 'p'")]
    [InlineData("create user u for login l")]
    [InlineData("alter role r add member u")]
    [InlineData("create server role sr")]
    public void Principal_syntax_is_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Fact]
    public void Principal_statements_split_without_semicolons()
    {
        const string sql = "create login l with password = 'p' create user u for login l";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(TwoItems, batch.Statements.Count);
        Assert.All(batch.Statements, statement => Assert.IsType<TsqlCreatePrincipalStatement>(statement));
    }

    [Fact]
    public void Visitor_walks_security_statements()
    {
        var visitor = new SecurityRecorder();
        visitor.Visit(Any("grant select on object::dbo.t to u"));
        visitor.Visit(Any("create login l with password = 'p'"));
        visitor.Visit(Any("alter role r add member u"));
        visitor.Visit(Any("execute as user = 'u'"));
        visitor.Visit(Any(RevertWord));
        visitor.Visit(Any("drop login l"));
        Assert.Equal(["TsqlPermission", "TsqlSecurable", "TsqlObjectOption"], visitor.Types);
    }

    private static Query Any(string sql)
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        Assert.Null(result.Error);
        return result.Root!;
    }

    private sealed class SecurityRecorder : SqlVisitor
    {
        private readonly List<string> _types = [];

        public IReadOnlyList<string> Types => _types;

        public override void VisitTsqlPermission(TsqlPermission node)
        {
            _types.Add(nameof(TsqlPermission));
            base.VisitTsqlPermission(node);
        }

        public override void VisitTsqlSecurable(TsqlSecurable node)
        {
            _types.Add(nameof(TsqlSecurable));
            base.VisitTsqlSecurable(node);
        }

        public override void VisitTsqlObjectOption(TsqlObjectOption node)
        {
            _types.Add(nameof(TsqlObjectOption));
            base.VisitTsqlObjectOption(node);
        }

        public override void VisitIdentifierExpression(IdentifierExpression node)
        {
            _types.Add(nameof(IdentifierExpression));
            base.VisitIdentifierExpression(node);
        }
    }
}
