namespace QlParse.Tests;

// SQL Server table DDL (stream D): CREATE TABLE columns, column properties, constraints and the table tail.
// ALTER TABLE, indexes, other objects and graph queries live in the other TsqlDdlTests partial files.
public sealed partial class TsqlDdlTests
{
    private const string Primary = "[PRIMARY]";
    private const string With = "with";
    private const string All = "all";
    private const string As = "as";
    private const string Clustered = "clustered";
    private const string ColumnC = "c";
    private const string FileGroup = "fg";
    private const string Hash = "hash";
    private const string Nonclustered = "nonclustered";
    private const string Null = "null";
    private const string On = "on";
    private const string One = "1";
    private const string Online = "online";
    private const string P2 = "p2";
    private const string Pf = "pf";
    private const int Third = 2;
    private const int Fourth = 3;
    private const int Fifth = 4;

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Text(string sql, SyntaxToken token) => Text(sql, token.Span);

    private static string Text(string sql, SyntaxToken? token) => token is { } value ? Text(sql, value.Span) : string.Empty;

    private static string Joined(string sql, IEnumerable<SyntaxToken> tokens) => string.Join(" ", tokens.Select(token => Text(sql, token)));

    // Parses under SqlServer, checks the root spans the whole input, and walks it with the default visitor.
    private static T ParseWhole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParseTsql<T>(sql);
        Assert.Equal(sql, Text(sql, node.Span));
        new SqlVisitor().Visit(node);
        return node;
    }

    private static ColumnDefinition Column(CreateTableStatement table, int index) =>
        Assert.IsType<ColumnDefinition>(table.Elements![index]);

    // ---- Columns ----

    [Fact]
    public void Parses_identity_with_seed_increment_and_not_for_replication()
    {
        const string Sql = "create table t (id int identity(1, -1) not for replication not null, b int identity)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var identity = Column(table, 0).SqlServer!.Identity!;
        Assert.Equal(One, Text(Sql, Assert.IsType<LiteralExpression>(identity.Seed).Literal));
        Assert.IsType<UnaryExpression>(identity.Increment);
        Assert.Equal("not for replication", Text(Sql, identity.NotForReplication!.Span));
        Assert.Equal("not null", Text(Sql, Column(table, 0).Constraints[0].Span));
        Assert.Null(Column(table, 1).SqlServer!.Identity!.OpenParen);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_computed_columns()
    {
        const string Sql = "create table t (a int, b as (a * 2) persisted not null, c as a + 1)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var persisted = Column(table, 1);
        Assert.Null(persisted.Type);
        Assert.Equal("persisted", Text(Sql, persisted.SqlServer!.Computed!.PersistedKeyword));
        Assert.IsType<ParenExpression>(persisted.SqlServer.Computed.Expression);
        Assert.Equal("not", Text(Sql, persisted.Constraints[0].NotKeyword));
        Assert.IsType<BinaryExpression>(Column(table, Third).SqlServer!.Computed!.Expression);
        Assert.Null(Column(table, Third).SqlServer!.Computed!.PersistedKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_column_storage_properties()
    {
        const string Sql = "create table t (a varbinary(10) filestream, b int sparse null, c uniqueidentifier rowguidcol, "
            + "cs xml column_set for all_sparse_columns, d varchar(10) collate Latin1_General_CI_AS)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("filestream", Text(Sql, Column(table, 0).SqlServer!.FilestreamKeyword));
        Assert.Equal("sparse", Text(Sql, Column(table, 1).SqlServer!.SparseKeyword));
        Assert.Equal(Null, Text(Sql, Column(table, 1).Constraints[0].NullKeyword));
        Assert.Null(Column(table, 1).Constraints[0].NotKeyword);
        Assert.Equal("rowguidcol", Text(Sql, Column(table, Third).SqlServer!.RowguidcolKeyword));
        var columnSet = Column(table, Fourth).SqlServer!;
        Assert.Equal("column_set for all_sparse_columns", $"{Text(Sql, columnSet.ColumnSetKeyword)} {Text(Sql, columnSet.ColumnSetForKeyword)} {Text(Sql, columnSet.AllSparseColumnsKeyword)}");
        Assert.Equal("Latin1_General_CI_AS", Text(Sql, Column(table, Fifth).Collation![0]));
        Assert.Null(Column(table, Fifth).SqlServer);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_named_inline_default_with_values()
    {
        const string Sql = "create table t (a int constraint df_a default 0 with values, b int default (getdate()))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var named = Column(table, 0);
        Assert.Equal("df_a", Text(Sql, named.SqlServer!.DefaultConstraintName));
        Assert.Equal("values", Text(Sql, named.SqlServer.DefaultValuesKeyword));
        Assert.Equal("default 0", Text(Sql, named.Default!.Span));
        Assert.NotNull(Column(table, 1).Default);
        Assert.Null(Column(table, 1).SqlServer);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Plain_column_keeps_iso_shape_without_extras()
    {
        const string Sql = "create table t (a int not null, b varchar(10) default 'x')";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Null(table.SqlServer);
        Assert.Null(Column(table, 0).SqlServer);
        Assert.Null(Column(table, 0).Postgres);
        Assert.Equal(Count.Two, table.Elements!.Count);
    }

    [Fact]
    public void Parses_dynamic_data_masking()
    {
        const string Sql = "create table t (email varchar(100) masked with (function = 'email()') null, ssn char(11) masked with (function = 'partial(0,\"XXX-XX-\",4)'))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var masking = Column(table, 0).SqlServer!.Masking!;
        Assert.Equal("'email()'", Text(Sql, masking.Function));
        Assert.Equal("function", Text(Sql, masking.FunctionKeyword));
        Assert.Equal(Null, Text(Sql, Column(table, 0).Constraints[0].NullKeyword));
        Assert.StartsWith("'partial", Text(Sql, Column(table, 1).SqlServer!.Masking!.Function));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_always_encrypted_column()
    {
        const string Sql = "create table t (ssn char(11) collate Latin1_General_BIN2 encrypted with (column_encryption_key = cek1, "
            + "encryption_type = deterministic, algorithm = 'AEAD_AES_256_CBC_HMAC_SHA_256') not null)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var options = Column(table, 0).SqlServer!.Encryption!.Options.Options;
        Assert.Equal(Count.Three, options.Count);
        Assert.Equal("column_encryption_key", Text(Sql, options[0].Name));
        Assert.Equal("cek1", Text(Sql, options[0].Value[0]));
        Assert.Equal("'AEAD_AES_256_CBC_HMAC_SHA_256'", Text(Sql, options[Third].Value[0]));
        SqlAssert.ParseFails(Sql);
    }

    // ---- Temporary tables ----

    [Theory]
    [InlineData("create table #t (a int)", "#t")]
    [InlineData("create table ##t (a int)", "##t")]
    [InlineData("create table [#t x] (a int)", "[#t x]")]
    public void Parses_temporary_table_names(string sql, string expectedName)
    {
        var table = ParseWhole<CreateTableStatement>(sql);
        Assert.Equal(expectedName, Text(sql, table.Name[0]));
        Assert.Null(table.TemporaryKeyword);
    }

    [Fact]
    public void Parses_table_variable_columns_with_tsql_properties()
    {
        const string Sql = "declare @t table (id int identity(1, 1) primary key nonclustered, name varchar(10) not null, index ix (name))";
        var declare = SqlAssert.ParseTsql<TsqlDeclareStatement>(Sql);
        new SqlVisitor().Visit(declare);
        var table = Assert.Single(declare.Variables).Table!;
        var id = Assert.IsType<ColumnDefinition>(table.Elements[0]);
        Assert.Equal(One, Text(Sql, Assert.IsType<LiteralExpression>(id.SqlServer!.Identity!.Increment).Literal));
        Assert.Equal(Nonclustered, Text(Sql, id.Constraints[0].SqlServer!.ClusteredKeyword));
        Assert.IsType<TsqlTableIndex>(table.Elements[Third]);
    }

    [Fact]
    public void Rejects_temporary_table_name_without_sql_server() =>
        SqlAssert.ParseFails("create table #t (a int)");

    // ---- Constraints ----

    [Fact]
    public void Parses_clustered_primary_key_with_options_and_storage()
    {
        const string Sql = "create table t (a int, b int, constraint pk primary key clustered (a desc, b) with (fillfactor = 80, pad_index = on) on [PRIMARY])";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var constraint = Assert.IsType<TableConstraint>(table.Elements![Third]);
        Assert.Equal("pk", Text(Sql, constraint.ConstraintName));
        Assert.Equal("a b", Joined(Sql, constraint.Columns!));
        var extras = constraint.SqlServer!;
        Assert.Equal(Clustered, Text(Sql, extras.ClusteredKeyword));
        Assert.Equal("desc", Text(Sql, extras.ColumnDirections![0]));
        Assert.Null(extras.ColumnDirections[1]);
        Assert.Equal(Count.Two, extras.Options!.Options.Count);
        Assert.Equal(On, Text(Sql, extras.Options.Options[1].Value[0]));
        Assert.Equal(Primary, Text(Sql, extras.Storage!.Target));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_nonclustered_unique_with_legacy_fillfactor()
    {
        const string Sql = "create table t (a int, unique nonclustered (a) with fillfactor = 70)";
        var constraint = Assert.IsType<TableConstraint>(ParseWhole<CreateTableStatement>(Sql).Elements![1]);
        Assert.Equal(Nonclustered, Text(Sql, constraint.SqlServer!.ClusteredKeyword));
        Assert.Null(constraint.SqlServer.Options!.OpenParen);
        Assert.Equal("fillfactor", Text(Sql, constraint.SqlServer.Options.Options[0].Name));
        Assert.Null(constraint.SqlServer.ColumnDirections);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_column_level_primary_key_and_references()
    {
        const string Sql = "create table t (id int constraint pk primary key nonclustered, r int references p (id) on delete set null on update cascade not for replication)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var key = Column(table, 0).Constraints[0];
        Assert.Equal("primary", Text(Sql, key.PrimaryKeyword));
        Assert.Equal(Nonclustered, Text(Sql, key.SqlServer!.ClusteredKeyword));
        var reference = Column(table, 1).Constraints[0];
        Assert.Equal("p", Text(Sql, reference.ReferenceTable![0]));
        Assert.Equal(Count.Two, reference.Actions.Count);
        Assert.Equal("not for replication", Text(Sql, reference.SqlServer!.NotForReplication!.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_foreign_key_and_check_not_for_replication()
    {
        const string Sql = "create table t (a int, constraint fk foreign key (a) references r (id) not for replication, constraint ck check not for replication (a > 0))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var foreignKey = Assert.IsType<TableConstraint>(table.Elements![1]);
        Assert.Equal("foreign", Text(Sql, foreignKey.ForeignKeyword));
        Assert.NotNull(foreignKey.SqlServer!.NotForReplication);
        var check = Assert.IsType<TableConstraint>(table.Elements[Third]);
        Assert.IsType<BinaryExpression>(check.Check);
        Assert.Equal("replication", Text(Sql, check.SqlServer!.NotForReplication!.ReplicationKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Plain_constraints_carry_no_extras()
    {
        const string Sql = "create table t (a int, primary key (a), check (a > 0), foreign key (a) references r)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.All(table.Elements!.Skip(1), element => Assert.Null(Assert.IsType<TableConstraint>(element).SqlServer));
    }

    // ---- Inline indexes ----

    [Fact]
    public void Parses_inline_table_and_column_indexes()
    {
        const string Sql = "create table t (a int index ix_a nonclustered, b int, index ix_b unique clustered (b desc) include (a) where b > 0 with (data_compression = row) on fg)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var columnIndex = Column(table, 0).SqlServer!.Index!;
        Assert.Equal("ix_a", Text(Sql, columnIndex.Name));
        Assert.Null(columnIndex.Columns);
        var index = Assert.IsType<TsqlTableIndex>(table.Elements![Third]);
        Assert.Equal("unique", Text(Sql, index.UniqueKeyword));
        Assert.Equal(Clustered, Text(Sql, index.ClusteredKeyword));
        Assert.Equal("desc", Text(Sql, index.Columns![0].Direction));
        Assert.Equal("a", Text(Sql, index.Include!.Columns[0]));
        Assert.IsType<BinaryExpression>(index.Where);
        Assert.Equal(FileGroup, Text(Sql, index.Storage!.Target));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_memory_optimized_table_with_hash_indexes()
    {
        const string Sql = "create table t (id int not null primary key nonclustered hash with (bucket_count = 1024), "
            + "name varchar(10) index ix_name hash with (bucket_count = 64), index cci clustered columnstore) "
            + "with (memory_optimized = on, durability = schema_and_data)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var key = Column(table, 0).Constraints[1];
        Assert.Equal(Hash, Text(Sql, key.SqlServer!.HashKeyword));
        Assert.Equal("1024", Text(Sql, key.SqlServer.Options!.Options[0].Value[0]));
        Assert.Equal(Hash, Text(Sql, Column(table, 1).SqlServer!.Index!.HashKeyword));
        Assert.Equal("columnstore", Text(Sql, Assert.IsType<TsqlTableIndex>(table.Elements![Third]).ColumnstoreKeyword));
        var options = table.SqlServer!.Options!.Options;
        Assert.Equal("memory_optimized", Text(Sql, options[0].Name));
        Assert.Equal("schema_and_data", Text(Sql, options[1].Value[0]));
        SqlAssert.ParseFails(Sql);
    }

    // ---- Table tail ----

    [Fact]
    public void Parses_storage_clauses()
    {
        const string Sql = "create table t (id int, doc varchar(10)) on ps (id) textimage_on [PRIMARY] filestream_on fs";
        var extras = ParseWhole<CreateTableStatement>(Sql).SqlServer!;
        Assert.Equal("ps", Text(Sql, extras.Storage!.Target));
        Assert.Equal("id", Text(Sql, extras.Storage.Column));
        Assert.Equal(Primary, Text(Sql, extras.TextImageOn!.Target));
        Assert.Equal("filestream_on", Text(Sql, extras.FilestreamOn!.Keyword));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create table t (a int) on \"default\"", "\"default\"")]
    [InlineData("create table t (a int) on [default]", "[default]")]
    [InlineData("create table t (a int) with (data_compression = page) on fg", "fg")]
    public void Parses_on_storage_targets(string sql, string expectedTarget) =>
        Assert.Equal(expectedTarget, Text(sql, ParseWhole<CreateTableStatement>(sql).SqlServer!.Storage!.Target));

    [Fact]
    public void Parses_data_compression_on_partitions()
    {
        const string Sql = "create table t (a int) with (data_compression = page on partitions (1, 3 to 5), xml_compression = on on partitions (2))";
        var options = ParseWhole<CreateTableStatement>(Sql).SqlServer!.Options!.Options;
        var ranges = options[0].Partitions!.Ranges;
        Assert.Equal("page", Text(Sql, options[0].Value[0]));
        Assert.Equal(Count.Two, ranges.Count);
        Assert.Equal("5", Text(Sql, ranges[1].End));
        Assert.Null(ranges[0].ToKeyword);
        Assert.Equal(On, Text(Sql, options[1].Value[0]));
        Assert.Equal("2", Text(Sql, options[1].Partitions!.Ranges[0].Start));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_system_versioned_temporal_table()
    {
        const string Sql = "create table t (id int primary key, vf datetime2 generated always as row start hidden not null, "
            + "vt datetime2 generated always as row end hidden not null, period for system_time (vf, vt)) "
            + "with (system_versioning = on (history_table = dbo.t_history, history_retention_period = 6 months))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var start = Column(table, 1);
        Assert.Equal("row", Text(Sql, start.Generated!.RowKeyword));
        Assert.Equal("start", Text(Sql, start.Generated.Bound));
        Assert.Equal("hidden", Text(Sql, start.SqlServer!.HiddenKeyword));
        Assert.Equal("end", Text(Sql, Column(table, Third).Generated!.Bound));
        Assert.Equal("system_time", Text(Sql, Assert.IsType<PeriodDefinition>(table.Elements![Fourth]).Name));
        var versioning = table.SqlServer!.Options!.Options[0];
        Assert.Equal(On, Text(Sql, versioning.Value[0]));
        var nested = versioning.Options!.Options;
        Assert.Equal("dbo . t_history", Joined(Sql, nested[0].Value));
        Assert.Equal("6 months", Joined(Sql, nested[1].Value));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_ledger_table_with_transaction_id_columns()
    {
        const string Sql = "create table t (a int, ts bigint generated always as transaction_id start hidden not null, "
            + "sq bigint generated always as sequence_number end hidden) with (ledger = on (append_only = on))";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("transaction_id", Text(Sql, Column(table, 1).Generated!.RowKeyword));
        Assert.Equal("sequence_number", Text(Sql, Column(table, Third).Generated!.RowKeyword));
        var ledger = table.SqlServer!.Options!.Options[0];
        Assert.Equal("ledger", Text(Sql, ledger.Name));
        Assert.Equal("append_only", Text(Sql, ledger.Options!.Options[0].Name));
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create table t (a int) with (memory_optimized = on)")]
    [InlineData("create table t (a int) on fg")]
    [InlineData("create table t (a int identity)")]
    [InlineData("create table t (a int sparse)")]
    [InlineData("create table t (a as b)")]
    public void Rejects_tsql_table_syntax_without_sql_server(string sql) => SqlAssert.ParseFails(sql);

    [Fact]
    public void Iso_create_table_is_unchanged_without_sql_server()
    {
        var table = SqlAssert.Parse<CreateTableStatement>("create table t (a integer generated always as identity, b int default 0 not null)");
        Assert.Null(table.SqlServer);
        Assert.NotNull(Assert.IsType<ColumnDefinition>(table.Elements![0]).Identity);
    }
}
