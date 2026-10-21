namespace QlParse.Tests;

// SQL Server gap closing (workstream H): DROP for the Service Broker, server, full-text and CLR kinds,
// ALTER FULLTEXT INDEX, ALTER VIEW, ALTER SEQUENCE and statement boundaries without `;`.
public sealed partial class TsqlGapsTests
{
    private const string AlterViewSql = "alter view dbo.v (a, b) with schemabinding, view_metadata as select a, b from dbo.t with check option";
    private const string AlterSequenceSql = "alter sequence dbo.s restart with 10 increment by 2 no cache cycle";

    [Theory]
    [InlineData("drop queue dbo.q", "queue", 1, "")]
    [InlineData("drop service [//x/s]", "service", 1, "")]
    [InlineData("drop contract [//x/c]", "contract", 1, "")]
    [InlineData("drop message type [//x/m]", "message type", 1, "")]
    [InlineData("drop route r", "route", 1, "")]
    [InlineData("drop remote service binding b", "remote service binding", 1, "")]
    [InlineData("drop broker priority p", "broker priority", 1, "")]
    [InlineData("drop endpoint e", "endpoint", 1, "")]
    [InlineData("drop availability group ag", "availability group", 1, "")]
    [InlineData("drop event session s on server", "event session", 1, "server")]
    [InlineData("drop event notification n, m on queue dbo.q", "event notification", 2, "queue dbo q")]
    [InlineData("drop resource pool p", "resource pool", 1, "")]
    [InlineData("drop external resource pool p", "external resource pool", 1, "")]
    [InlineData("drop workload group g", "workload group", 1, "")]
    [InlineData("drop workload classifier c", "workload classifier", 1, "")]
    [InlineData("drop fulltext catalog c", "fulltext catalog", 1, "")]
    [InlineData("drop fulltext index on dbo.t", "fulltext index", 0, "dbo t")]
    [InlineData("drop fulltext stoplist s", "fulltext stoplist", 1, "")]
    [InlineData("drop search property list p", "search property list", 1, "")]
    [InlineData("drop aggregate dbo.a", "aggregate", 1, "")]
    [InlineData("drop external language l", "external language", 1, "")]
    [InlineData("drop external library l", "external library", 1, "")]
    public void Parses_drop_for_every_object_kind(string sql, string kind, int names, string scope)
    {
        var drop = Whole<TsqlDropObjectsStatement>(sql);
        Assert.Equal(kind, Words(sql, drop.ObjectKind));
        Assert.Equal(names, drop.Names.Count);
        Assert.Equal(scope, Words(sql, drop.Scope));
        Assert.Equal(scope.Length > 0, drop.OnKeyword is not null);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("drop assembly a with no dependents", "with no dependents")]
    [InlineData("drop symmetric key k remove provider key", "remove provider key")]
    public void Drop_keeps_trailing_options(string sql, string options) =>
        Assert.Equal(options, Words(sql, Whole<TsqlDropSecurableStatement>(sql).Options));

    [Fact]
    public void Drop_event_session_needs_its_scope_word()
    {
        SqlAssert.ParseFails("drop event session s on queue", SqlOptions.SqlServer);
        SqlAssert.ParseFails("drop fulltext index t", SqlOptions.SqlServer);
        SqlAssert.ParseFails("drop message s", SqlOptions.SqlServer);
    }

    [Theory]
    [InlineData("alter fulltext index on dbo.t enable", "enable", 0, false, false)]
    [InlineData("alter fulltext index on t disable", "disable", 0, false, false)]
    [InlineData("alter fulltext index on t set change_tracking = auto", "set", 0, true, false)]
    [InlineData("alter fulltext index on t set change_tracking off", "set", 0, true, false)]
    [InlineData("alter fulltext index on t set stoplist = system with no population", "set", 0, true, true)]
    [InlineData("alter fulltext index on t set search property list = p", "set", 0, true, false)]
    [InlineData("alter fulltext index on t add (c1 language 1033, c2 type column ext) with no population", "add", 2, false, true)]
    [InlineData("alter fulltext index on t drop (c1, c2)", "drop", 2, false, false)]
    [InlineData("alter fulltext index on t alter column c add statistical_semantics", "alter column c add statistical_semantics", 0, false, false)]
    [InlineData("alter fulltext index on t start full population", "start full population", 0, false, false)]
    [InlineData("alter fulltext index on t start incremental population", "start incremental population", 0, false, false)]
    [InlineData("alter fulltext index on t start update population", "start update population", 0, false, false)]
    [InlineData("alter fulltext index on t stop population", "stop population", 0, false, false)]
    [InlineData("alter fulltext index on t pause population", "pause population", 0, false, false)]
    [InlineData("alter fulltext index on t resume population", "resume population", 0, false, false)]
    public void Parses_alter_fulltext_index(string sql, string action, int columns, bool option, bool noPopulation)
    {
        var index = Whole<TsqlAlterFulltextIndexStatement>(sql);
        Assert.Equal(action, Words(sql, index.Action));
        Assert.Equal(columns, index.Columns.Count);
        Assert.Equal(option, index.Option is not null);
        Assert.Equal(noPopulation, index.NoPopulation.Count > 0);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Alter_fulltext_index_rejects_unknown_actions()
    {
        SqlAssert.ParseFails("alter fulltext index on t rebuild", SqlOptions.SqlServer);
        SqlAssert.ParseFails("alter fulltext index on t start population", SqlOptions.SqlServer);
    }

    [Fact]
    public void Alter_view_takes_the_create_view_grammar()
    {
        var alter = Whole<TsqlAlterViewStatement>(AlterViewSql);
        Assert.Equal("alter", Text(AlterViewSql, alter.View.CreateKeyword));
        Assert.Equal("schemabinding view_metadata", Words(AlterViewSql, alter.View.SqlServer!.Attributes));
        Assert.Equal("a b", Words(AlterViewSql, alter.View.Columns!));
        Assert.NotNull(alter.View.CheckKeyword);
        Assert.Null(Whole<TsqlAlterViewStatement>("alter view v as select 1 as a").View.SqlServer);
        Assert.IsType<AlterViewStatement>(SqlAssert.Parse<AlterViewStatement>("alter view v as select 1"));
        SqlAssert.ParseFails(AlterViewSql);
    }

    [Fact]
    public void Alter_sequence_takes_t_sql_options()
    {
        var sequence = Whole<AlterSequenceStatement>(AlterSequenceSql);
        Assert.Equal(4, sequence.Options.Count);
        Assert.Equal("restart with 10", Text(AlterSequenceSql, sequence.Options[0].Span));
        Assert.Equal("no cache", Text(AlterSequenceSql, sequence.Options[^2].Span));
        SqlAssert.ParseFails(AlterSequenceSql);
    }

    [Theory]
    [InlineData("backup certificate c to file = 'c.cer' with private key (file = 'c.pvk', encryption by password = 'p')", "backup certificate", 2)]
    [InlineData("backup master key to file = 'k' encryption by password = 'p'", "backup master key", 2)]
    [InlineData("backup service master key to file = 'k' encryption by password = 'p'", "backup service master key", 2)]
    [InlineData("backup symmetric key k to file = 'k' encryption by password = 'p'", "backup symmetric key", 2)]
    [InlineData("restore master key from file = 'k' decryption by password = 'p' encryption by password = 'q' force", "restore master key", 4)]
    public void Parses_backup_and_restore_of_keys(string sql, string keywords, int clauses)
    {
        var statement = Whole<TsqlSecurityObjectStatement>(sql);
        Assert.Equal(keywords, Words(sql, statement.Keywords));
        Assert.Equal(clauses, statement.Clauses.Count);
        SqlAssert.ParseFails(sql);
    }

    [Theory]
    [InlineData("select * from changetable(changes dbo.t, @v) as c", "changes")]
    [InlineData("select * from changetable(version dbo.t, (id), (@id)) as c", "version")]
    public void Parses_changetable(string sql, string kind)
    {
        var table = Assert.IsType<TsqlRowsetFunctionTable>(SqlAssert.SelectTsql(sql).From);
        Assert.Equal(kind, Text(sql, table.Arguments[0].Name!.Value));
        Assert.Equal("c", Text(sql, table.Alias!.Value));
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Visitor_walks_gap_statements()
    {
        var visitor = new CountingVisitor();
        visitor.Visit(SqlAssert.ParseTsql<TsqlAlterViewStatement>(AlterViewSql));
        visitor.Visit(SqlAssert.ParseTsql<TsqlAlterFulltextIndexStatement>("alter fulltext index on t set stoplist off"));
        Assert.Equal(1, visitor.Views);
        Assert.Equal(1, visitor.Options);
    }

    // ---- Statement boundaries without `;` ----

    [Theory]
    [InlineData("select 1 select 2", "SelectStatement,SelectStatement")]
    [InlineData("if @x = 1 select 1 else select 2 print 'x'", "TsqlIfStatement,TsqlPrintStatement")]
    [InlineData("begin tran update t set a = 1 commit", "TsqlTransactionStatement,TsqlUpdateStatement,TsqlTransactionStatement")]
    [InlineData("return select 1", "TsqlReturnStatement,SelectStatement")]
    [InlineData("return @x + 1 select 2", "TsqlReturnStatement,SelectStatement")]
    [InlineData("waitfor delay '00:00:01' select 1", "TsqlWaitforStatement,SelectStatement")]
    [InlineData("declare @a int set @a = 1", "TsqlDeclareStatement,TsqlSetVariableStatement")]
    [InlineData("lbl: select 1 goto lbl", "TsqlLabelStatement,SelectStatement,TsqlGotoStatement")]
    [InlineData("while @i < 10 set @i += 1 select @i", "TsqlWhileStatement,SelectStatement")]
    [InlineData("insert into t select 1 select 2", "TsqlInsertStatement,SelectStatement")]
    [InlineData("exec p 1, 2 select 3", "TsqlExecuteStatement,SelectStatement")]
    [InlineData("rollback transaction select 1", "TsqlTransactionStatement,SelectStatement")]
    [InlineData("select * from t t2 select 1", "SelectStatement,SelectStatement")]
    [InlineData("merge t using s on t.a = s.a when matched then delete; select 1", "TsqlMergeStatement,SelectStatement")]
    [InlineData("select a from t order by a fetch next from c", "SelectStatement,TsqlFetchStatement")]
    [InlineData("(select 1) order by 1 select 2", "OrderedQuery,SelectStatement")]
    [InlineData("alter fulltext index on t enable select 1", "TsqlAlterFulltextIndexStatement,SelectStatement")]
    [InlineData("drop queue q drop service s", "TsqlDropObjectsStatement,TsqlDropObjectsStatement")]
    public void Splits_statements_without_terminators(string sql, string statements)
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(statements, string.Join(",", batch.Statements.Select(statement => statement.GetType().Name)));
    }

    private sealed class CountingVisitor : SqlVisitor
    {
        public int Views { get; private set; }

        public int Options { get; private set; }

        public override void VisitCreateViewStatement(CreateViewStatement node)
        {
            Views++;
            base.VisitCreateViewStatement(node);
        }

        public override void VisitTsqlOption(TsqlOption node)
        {
            Options++;
            base.VisitTsqlOption(node);
        }
    }
}
