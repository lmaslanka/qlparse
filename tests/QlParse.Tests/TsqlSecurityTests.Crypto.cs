namespace QlParse.Tests;

// Workstream F: SQL Server keys and certificates, Always Encrypted keys, credentials, auditing, module signatures,
// row-level security policies and DROP of the security objects.
public sealed partial class TsqlSecurityTests
{
    private const string EncryptionBy = "encryption by";

    // ---- Keys and certificates ----------------------------------------------------------------

    [Fact]
    public void Parses_master_key_statements()
    {
        const string sql = "create master key encryption by password = 'p'";
        var create = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal("create master key", Words(sql, create.Keywords));
        Assert.Null(create.Name);
        var clause = Assert.Single(create.Clauses);
        Assert.Equal(EncryptionBy, Words(sql, clause));
        var password = Assert.Single(clause.Options!.Items);
        Assert.Equal("password", Words(sql, password));
        Assert.Equal("'p'", Value(sql, password));
        const string openSql = "open master key decryption by password = 'p'";
        Assert.Equal("decryption by", Words(openSql, Whole<TsqlSecurityObjectStatement>(openSql).Clauses[0]));
        Assert.Empty(Whole<TsqlSecurityObjectStatement>("close master key").Clauses);
        const string regenerateSql = "alter master key force regenerate with encryption by password = 'p'";
        var regenerate = Whole<TsqlSecurityObjectStatement>(regenerateSql);
        Assert.Equal(["force", "regenerate", "with"], regenerate.Clauses.Select(item => Words(regenerateSql, item)));
        Assert.Equal("encryption by password", Words(regenerateSql, regenerate.Clauses[^1].Options!.Items[0]));
        const string serviceSql = "alter master key add encryption by service master key";
        var service = Whole<TsqlSecurityObjectStatement>(serviceSql).Clauses[0];
        Assert.Equal("add encryption by", Words(serviceSql, service));
        Assert.Equal("service master key", Words(serviceSql, service.Options!.Items[0]));
    }

    [Fact]
    public void Parses_certificates()
    {
        const string sql = "create certificate c authorization u from file = 'c:\\c.cer' "
            + "with private key (file = 'c:\\c.pvk', decryption by password = 'p')";
        var create = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal(CertificateName, Text(sql, create.Name));
        Assert.Equal(["authorization", "from file", "with private key"], create.Clauses.Select(item => Words(sql, item)));
        Assert.Equal(Principal, Value(sql, create.Clauses[0]));
        Assert.Equal("decryption by password", Words(sql, create.Clauses[^1].Options!.Items[1]));
        const string subjectSql = "create certificate c encryption by password = 'p' with subject = 's', expiry_date = '20301031' "
            + "active for begin_dialog = on";
        var subject = Whole<TsqlSecurityObjectStatement>(subjectSql);
        Assert.Equal([EncryptionBy, "with", "active for begin_dialog"], subject.Clauses.Select(item => Words(subjectSql, item)));
        Assert.Equal(TwoItems, subject.Clauses[1].Options!.Items.Count);
        const string executableSql = "create certificate c from executable file = 'x.dll'";
        Assert.Equal("from executable file", Words(executableSql, Whole<TsqlSecurityObjectStatement>(executableSql).Clauses[0]));
        const string removeSql = "alter certificate c remove private key";
        Assert.Equal("remove private key", Words(removeSql, Whole<TsqlSecurityObjectStatement>(removeSql).Clauses[0]));
    }

    [Fact]
    public void Parses_symmetric_and_asymmetric_keys()
    {
        const string sql = "create symmetric key k with algorithm = aes_256, key_source = 'x' encryption by password = 'p', certificate c";
        var key = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal("create symmetric key", Words(sql, key.Keywords));
        Assert.Equal(["with", EncryptionBy], key.Clauses.Select(item => Words(sql, item)));
        Assert.Equal("aes_256", Value(sql, key.Clauses[0].Options!.Items[0]));
        var mechanisms = key.Clauses[1].Options!.Items;
        Assert.Equal(["password", "certificate"], mechanisms.Select(item => Words(sql, item)));
        Assert.Equal(CertificateName, Value(sql, mechanisms[1]));
        const string providerSql = "create symmetric key k from provider p with provider_key_name = 'n', creation_disposition = open_existing";
        Assert.Equal("p", Value(providerSql, Whole<TsqlSecurityObjectStatement>(providerSql).Clauses[0]));
        const string asymmetricSql = "create asymmetric key k from assembly a";
        var asymmetric = Whole<TsqlSecurityObjectStatement>(asymmetricSql);
        Assert.Equal("create asymmetric key", Words(asymmetricSql, asymmetric.Keywords));
        Assert.Equal("from assembly", Words(asymmetricSql, asymmetric.Clauses[0]));
    }

    [Fact]
    public void Parses_open_and_close_symmetric_keys()
    {
        const string sql = "open symmetric key k decryption by asymmetric key ak with password = 'p'";
        var open = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal(KeyName, Text(sql, open.Name));
        var mechanism = open.Clauses[0].Options!.Items[0];
        Assert.Equal("asymmetric key", Words(sql, mechanism));
        Assert.Equal("ak", Value(sql, mechanism));
        Assert.Equal("with password", Words(sql, mechanism.Options!.Items[0]));
        Assert.Equal(KeyName, Text("close symmetric key k", Whole<TsqlSecurityObjectStatement>("close symmetric key k").Name));
        const string allSql = "close all symmetric keys";
        var all = Whole<TsqlSecurityObjectStatement>(allSql);
        Assert.Equal(allSql, Words(allSql, all.Keywords));
        Assert.Null(all.Name);
    }

    [Fact]
    public void Parses_module_signatures()
    {
        const string sql = "add counter signature to object::dbo.p by certificate c, asymmetric key k with password = 'x'";
        var add = Whole<TsqlSignatureStatement>(sql);
        Assert.NotNull(add.CounterKeyword);
        Assert.Equal("object", Words(sql, add.Securable.ClassKeywords));
        Assert.Equal(["certificate", "asymmetric key"], add.Signers.Select(signer => Words(sql, signer)));
        Assert.NotNull(add.Signers[1].Options);
        const string dropSql = "drop signature from dbo.p by certificate c";
        var drop = Whole<TsqlSignatureStatement>(dropSql);
        Assert.Equal(DropWord, Text(dropSql, drop.VerbKeyword));
        Assert.Equal("from", Text(dropSql, drop.ToKeyword));
    }

    // ---- Always Encrypted keys ----------------------------------------------------------------

    [Fact]
    public void Parses_column_master_and_encryption_keys()
    {
        const string sql = "create column master key cmk with (key_store_provider_name = N'MSSQL_CERTIFICATE_STORE', key_path = N'CurrentUser/My/abc')";
        var master = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal("create column master key", Words(sql, master.Keywords));
        Assert.Equal(TwoItems, master.Clauses[0].Options!.Items.Count);
        const string cekSql = "create column encryption key cek with values (column_master_key = cmk, algorithm = 'RSA_OAEP', "
            + "encrypted_value = 0x016E), (column_master_key = cmk2, algorithm = 'RSA_OAEP', encrypted_value = 0x01)";
        var cek = Whole<TsqlSecurityObjectStatement>(cekSql);
        Assert.Equal(TwoItems, cek.Clauses.Count);
        Assert.Equal("with values", Words(cekSql, cek.Clauses[0]));
        Assert.Empty(cek.Clauses[1].Keywords);
        Assert.Equal("0x01", Value(cekSql, cek.Clauses[1].Options!.Items[^1]));
        const string addSql = "alter column encryption key cek add value (column_master_key = cmk2, algorithm = 'RSA_OAEP', encrypted_value = 0x01)";
        Assert.Equal("add value", Words(addSql, Whole<TsqlSecurityObjectStatement>(addSql).Clauses[0]));
    }

    // ---- Credentials and auditing -------------------------------------------------------------

    [Fact]
    public void Parses_credentials()
    {
        const string sql = "create credential c with identity = 'i', secret = 's' for cryptographic provider p";
        var credential = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal(["with", "for cryptographic provider"], credential.Clauses.Select(item => Words(sql, item)));
        Assert.Equal(TwoItems, credential.Clauses[0].Options!.Items.Count);
        const string scopedSql = "create database scoped credential c with identity = 'Managed Identity'";
        Assert.Equal("create database scoped credential", Words(scopedSql, Whole<TsqlSecurityObjectStatement>(scopedSql).Keywords));
        const string alterSql = "alter credential c with identity = 'j'";
        Assert.Equal("alter credential", Words(alterSql, Whole<TsqlSecurityObjectStatement>(alterSql).Keywords));
    }

    [Fact]
    public void Parses_server_audits()
    {
        const string sql = "create server audit a to file (filepath = 'c:\\a\\', maxsize = 10 mb, max_rollover_files = unlimited) "
            + "with (queue_delay = 1000, on_failure = continue) where object_name = 'x'";
        var audit = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal("create server audit", Words(sql, audit.Keywords));
        Assert.Equal(["to file", "with"], audit.Clauses.Select(item => Words(sql, item)));
        Assert.Equal("10 mb", Value(sql, audit.Clauses[0].Options!.Items[1]));
        Assert.IsType<BinaryExpression>(audit.Where);
        const string logSql = "create server audit a to application_log";
        Assert.Equal("to application_log", Words(logSql, Whole<TsqlSecurityObjectStatement>(logSql).Clauses[0]));
        const string renameSql = "alter server audit a modify name = b";
        Assert.Equal("modify name", Words(renameSql, Whole<TsqlSecurityObjectStatement>(renameSql).Clauses[0]));
        const string removeSql = "alter server audit a remove where";
        Assert.Equal("remove where", Words(removeSql, Whole<TsqlSecurityObjectStatement>(removeSql).Clauses[0]));
    }

    [Fact]
    public void Parses_audit_specifications()
    {
        const string sql = "create database audit specification s for server audit a "
            + "add (select, insert on object::dbo.t by public), add (database_object_change_group) with (state = on)";
        var spec = Whole<TsqlAuditSpecificationStatement>(sql);
        Assert.Equal("create database audit specification", Words(sql, spec.Keywords));
        Assert.Equal("a", Text(sql, spec.Audit));
        Assert.Equal(TwoItems, spec.Actions.Count);
        var first = spec.Actions[0];
        Assert.Equal(["select", "insert"], first.Actions.Select(action => Words(sql, action.Keywords)));
        Assert.Equal("dbo t", Words(sql, first.Securable!.Name));
        Assert.Equal("public", Text(sql, first.Principals[0]));
        Assert.Null(spec.Actions[1].Securable);
        Assert.Single(spec.Options!.Items);
        const string serverSql = "create server audit specification s for server audit a add (failed_login_group)";
        Assert.Equal("create server audit specification", Words(serverSql, Whole<TsqlAuditSpecificationStatement>(serverSql).Keywords));
        const string dropSql = "alter database audit specification s drop (select on object::dbo.t by public)";
        Assert.Equal(DropWord, Text(dropSql, Whole<TsqlAuditSpecificationStatement>(dropSql).Actions[0].ActionKeyword));
    }

    // ---- Row-level security -------------------------------------------------------------------

    [Fact]
    public void Parses_create_security_policy()
    {
        const string sql = "create security policy sp add filter predicate dbo.fn(region) on dbo.t, "
            + "add block predicate dbo.fn(region) on dbo.t after insert with (state = on, schemabinding = off) not for replication";
        var policy = Whole<TsqlSecurityPolicyStatement>(sql);
        Assert.Equal(TwoItems, policy.Predicates.Count);
        var filter = policy.Predicates[0];
        Assert.Equal("filter", Text(sql, filter.KindKeyword));
        Assert.Equal("dbo fn", Words(sql, filter.FunctionName));
        Assert.IsType<IdentifierExpression>(Assert.Single(filter.Arguments));
        Assert.Equal("dbo t", Words(sql, filter.Table));
        Assert.Equal("after insert", Words(sql, policy.Predicates[1].BlockOperation));
        Assert.Equal(TwoItems, policy.Options!.Items.Count);
        Assert.Equal("not for replication", Words(sql, policy.NotForReplication));
    }

    [Fact]
    public void Parses_alter_security_policy()
    {
        const string sql = "alter security policy sp add filter predicate dbo.fn(r) on dbo.t2, drop filter predicate on dbo.t";
        var policy = Whole<TsqlSecurityPolicyStatement>(sql);
        Assert.Equal("alter", Text(sql, policy.VerbKeyword));
        Assert.Empty(policy.Predicates[1].FunctionName);
        Assert.Null(policy.Predicates[1].OpenParen);
        Assert.Empty(Whole<TsqlSecurityPolicyStatement>("alter security policy sp with (state = off)").Predicates);
        const string beforeSql = "alter security policy sp alter block predicate dbo.fn2(a, b) on dbo.t before delete";
        var before = Whole<TsqlSecurityPolicyStatement>(beforeSql).Predicates[0];
        Assert.Equal(TwoItems, before.Arguments.Count);
        Assert.Equal("before delete", Words(beforeSql, before.BlockOperation));
    }

    // ---- DROP ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("drop login l", "login")]
    [InlineData("drop user if exists u", "user")]
    [InlineData("drop server role sr", "server role")]
    [InlineData("drop database scoped credential c", "database scoped credential")]
    [InlineData("drop server audit specification s", "server audit specification")]
    [InlineData("drop security policy if exists dbo.sp", "security policy")]
    [InlineData("drop external file format ff", "external file format")]
    [InlineData("drop xml schema collection dbo.c", "xml schema collection")]
    [InlineData("drop column encryption key cek", "column encryption key")]
    [InlineData("drop asymmetric key k", "asymmetric key")]
    [InlineData("drop rule r", "rule")]
    public void Parses_drop_of_security_objects(string sql, string kind)
    {
        var drop = Whole<TsqlDropSecurableStatement>(sql);
        Assert.Equal(kind, Words(sql, drop.KindKeywords));
        Assert.Single(drop.Names);
    }

    [Fact]
    public void Parses_drop_lists_if_exists_and_unnamed_keys()
    {
        const string sql = "drop database if exists a, b";
        var drop = Whole<TsqlDropSecurableStatement>(sql);
        Assert.NotNull(drop.ExistsKeyword);
        Assert.Equal(TwoItems, drop.Names.Count);
        Assert.Empty(Whole<TsqlDropSecurableStatement>("drop master key").Names);
        Assert.Empty(Whole<TsqlDropSecurableStatement>("drop database encryption key").Names);
    }

    [Theory]
    [InlineData("create master key encryption by password = 'p'")]
    [InlineData("create certificate c with subject = 's'")]
    [InlineData("open symmetric key k decryption by certificate c")]
    [InlineData("create credential c with identity = 'i'")]
    [InlineData("create security policy sp add filter predicate dbo.fn(a) on dbo.t")]
    [InlineData("add signature to p by certificate c")]
    [InlineData("drop login l")]
    [InlineData("create column master key k with (key_path = 'x')")]
    public void Key_and_audit_syntax_is_rejected_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Theory]
    [InlineData("create master key encryption by")]
    [InlineData("create certificate c from file =")]
    [InlineData("create symmetric key k encryption by certificate")]
    [InlineData("create security policy sp add filter predicate dbo.fn(a)")]
    [InlineData("add signature to p")]
    [InlineData("create database audit specification s add (select on object::t)")]
    [InlineData("alter certificate c remove")]
    public void Rejects_malformed_key_statements(string sql) => SqlAssert.ParseFails(sql, SqlOptions.SqlServer);

    [Fact]
    public void Visitor_walks_key_and_policy_statements()
    {
        var visitor = new SecurityRecorder();
        visitor.Visit(Any("create security policy sp add filter predicate dbo.fn(region) on dbo.t with (state = on)"));
        visitor.Visit(Any("create server audit a to file (filepath = 'x') where object_name = 'y'"));
        visitor.Visit(Any("create database audit specification s for server audit a add (select on object::t by public)"));
        visitor.Visit(Any("add signature to p by certificate c"));
        Assert.Equal(TwoItems, visitor.Types.Count(type => type == nameof(IdentifierExpression)));
        Assert.Equal(TwoItems, visitor.Types.Count(type => type == nameof(TsqlSecurable)));
        Assert.Equal(ThreeItems + 1, visitor.Types.Count(type => type == nameof(TsqlObjectOption)));
    }
}
