namespace QlParse.Tests;

// Postgres table DDL (stream D): CREATE TABLE extensions, partitioning, DROP forms, and the visitor
// walk over the new nodes. ALTER TABLE, indexes, views, sequences, and tablespaces live in the other
// PgDdlTests partial files.
public sealed partial class PgDdlTests
{
    private const string TableName = "t";
    private const string FastTablespace = "fast";

    private static string Text(string sql, SourceSpan span) => sql[span.Position..span.End];

    private static string Text(string sql, SyntaxToken token) => Text(sql, token.Span);

    private static string Text(string sql, SyntaxToken? token) => token is { } value ? Text(sql, value.Span) : string.Empty;

    private static T ParseWhole<T>(string sql) where T : Query
    {
        var node = SqlAssert.ParsePg<T>(sql);
        Assert.Equal(sql, Text(sql, node.Span));
        return node;
    }

    // ---- Tables and inheritance ----

    [Fact]
    public void Parses_create_table_inherits()
    {
        const string Sql = "create table t (a int) inherits (p, s.q)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal(Count.Two, table.Postgres!.Inherits!.Count);
        Assert.Equal(Count.Two, table.Postgres!.Inherits[1].Count);
        Assert.Equal("inherits", Text(Sql, table.Postgres!.InheritsKeyword));
        Assert.Equal(")", Text(Sql, table.Postgres!.InheritsClose));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_unlogged_table()
    {
        const string Sql = "create unlogged table t (a int)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("unlogged", Text(Sql, table.Postgres!.UnloggedKeyword));
        Assert.Null(table.TemporaryKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Theory]
    [InlineData("create temp table t (a int) on commit drop", "temp", "drop")]
    [InlineData("create temporary table if not exists t (a int) on commit delete rows", "temporary", "delete")]
    [InlineData("create temp table t (a int) on commit preserve rows", "temp", "preserve")]
    public void Parses_temporary_tables_with_on_commit(string sql, string expectedPersistence, string expectedAction)
    {
        var table = ParseWhole<CreateTableStatement>(sql);
        Assert.Equal(expectedPersistence, Text(sql, table.TemporaryKeyword));
        Assert.Equal(expectedAction, Text(sql, table.CommitAction));
        Assert.Null(table.Scope);
        SqlAssert.ParseFails(sql);
    }

    [Fact]
    public void Parses_create_table_if_not_exists()
    {
        const string Sql = "create table if not exists t (a int)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("if not exists", Text(Sql, table.Postgres!.IfNotExists!.Span));
        Assert.Equal(TableName, Text(Sql, table.Name[0]));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_as_without_with_data()
    {
        const string Sql = "create table t as select 1";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.IsType<SelectStatement>(table.Query);
        Assert.Null(table.DataKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_as_with_storage_and_no_data()
    {
        const string Sql = "create table t with (fillfactor = 70) as select 1 with no data";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Single(table.Postgres!.StorageParameters!.Parameters);
        Assert.NotNull(table.NoKeyword);
        Assert.Equal("data", Text(Sql, table.DataKeyword));
    }

    [Fact]
    public void Parses_like_including_all()
    {
        const string Sql = "create table t (like other including all excluding indexes)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var like = Assert.IsType<LikeClause>(Assert.Single(table.Elements!));
        Assert.Equal(Count.Two, like.Options.Count);
        Assert.Equal("all", Text(Sql, like.Options[0].Option));
        Assert.Equal("excluding indexes", Text(Sql, like.Options[1].Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_storage_parameters()
    {
        const string Sql = "create table t (a int) with (fillfactor = 70, toast.autovacuum_enabled = false)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var parameters = table.Postgres!.StorageParameters!.Parameters;
        Assert.Equal(Count.Two, parameters.Count);
        Assert.Equal("fillfactor = 70", Text(Sql, parameters[0].Span));
        Assert.Equal(Count.Two, parameters[1].Name.Count);
        Assert.Equal("false", Text(Sql, parameters[1].Value));
        Assert.Equal("with", Text(Sql, table.Postgres!.StorageWithKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_without_oids()
    {
        const string Sql = "create table t (a int) without oids";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("oids", Text(Sql, table.Postgres!.OidsKeyword));
        Assert.NotNull(table.Postgres!.WithoutKeyword);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_using_access_method()
    {
        const string Sql = "create table t (a int) using heap";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("heap", Text(Sql, table.Postgres!.AccessMethod));
        Assert.Equal("using", Text(Sql, table.Postgres!.UsingKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_create_table_tablespace()
    {
        const string Sql = "create table t (a int) with (fillfactor = 70) on commit delete rows tablespace fast";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal(FastTablespace, Text(Sql, table.Postgres!.Tablespace));
        Assert.Equal("rows", Text(Sql, table.RowsKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_empty_column_list_and_null_constraint()
    {
        const string Empty = "create table t ()";
        Assert.Empty(ParseWhole<CreateTableStatement>(Empty).Elements!);
        SqlAssert.ParseFails(Empty);
        const string Sql = "create table t (a int null, b text constraint nn null)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var column = Assert.IsType<ColumnDefinition>(table.Elements![0]);
        var constraint = Assert.Single(column.Constraints);
        Assert.Equal("null", Text(Sql, constraint.Span));
        Assert.Null(constraint.NotKeyword);
        SqlAssert.ParseFails(Sql);
    }

    // ---- Partitioning ----

    [Fact]
    public void Parses_partition_by_range()
    {
        const string Sql = "create table t (a int) partition by range (a)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var partitionBy = table.Postgres!.PartitionBy!;
        Assert.Equal("range", Text(Sql, partitionBy.Strategy));
        Assert.Equal("partition by range (a)", Text(Sql, partitionBy.Span));
        Assert.IsType<IdentifierExpression>(Assert.Single(partitionBy.Columns).Expression);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_by_list_expression()
    {
        const string Sql = "create table t (a int, b text) partition by list (lower(b))";
        var partitionBy = ParseWhole<CreateTableStatement>(Sql).Postgres!.PartitionBy!;
        Assert.Equal("list", Text(Sql, partitionBy.Strategy));
        Assert.Equal("lower(b)", Text(Sql, Assert.Single(partitionBy.Columns).Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_by_hash_with_opclass_and_collation()
    {
        const string Sql = @"create table t (a int, b text) partition by hash (a, (b || 'x') text_pattern_ops, b collate ""C"")";
        var partitionBy = ParseWhole<CreateTableStatement>(Sql).Postgres!.PartitionBy!;
        Assert.Equal("hash", Text(Sql, partitionBy.Strategy));
        Assert.Equal(Count.Three, partitionBy.Columns.Count);
        Assert.Equal("text_pattern_ops", Text(Sql, partitionBy.Columns[1].Postgres!.OperatorClass![0]));
        Assert.Equal("(b || 'x') text_pattern_ops", Text(Sql, partitionBy.Columns[1].Span));
        Assert.IsType<CollateExpression>(partitionBy.Columns[Count.Two].Expression);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_of_for_values_in()
    {
        const string Sql = "create table p1 partition of t for values in (1, 2)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        var partitionOf = table.Postgres!.PartitionOf!;
        Assert.Equal(TableName, Text(Sql, partitionOf.Parent[0]));
        Assert.Equal(Count.Two, partitionOf.Bound.Values.Count);
        Assert.Equal("for values in (1, 2)", Text(Sql, partitionOf.Bound.Span));
        Assert.Null(table.Elements);
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_of_for_values_from_to()
    {
        const string Sql = "create table p1 partition of t for values from (minvalue, 'a') to (100, maxvalue)";
        var bound = ParseWhole<CreateTableStatement>(Sql).Postgres!.PartitionOf!.Bound;
        Assert.Equal(Count.Two, bound.Values.Count);
        Assert.Equal(Count.Two, bound.ToValues.Count);
        Assert.Equal("minvalue", Text(Sql, bound.Values[0].Span));
        Assert.Equal("to", Text(Sql, bound.ToKeyword));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_partition_of_for_values_with_modulus()
    {
        const string Sql = "create table p1 partition of t for values with (modulus 4, remainder 0)";
        var bound = ParseWhole<CreateTableStatement>(Sql).Postgres!.PartitionOf!.Bound;
        var parameters = bound.HashParameters!.Parameters;
        Assert.Equal("modulus 4", Text(Sql, parameters[0].Span));
        Assert.Equal("0", Text(Sql, parameters[1].Value));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_default_partition_with_elements_and_subpartitioning()
    {
        const string Sql = "create table p1 partition of t (a not null, b with options default 0, constraint c check (a > 0)) default partition by range (a)";
        var table = ParseWhole<CreateTableStatement>(Sql);
        Assert.Equal("default", Text(Sql, table.Postgres!.PartitionOf!.Bound.DefaultKeyword));
        Assert.Equal(Count.Three, table.Elements!.Count);
        var column = Assert.IsType<ColumnDefinition>(table.Elements[0]);
        Assert.Null(column.Type);
        Assert.Equal("a not null", Text(Sql, column.Span));
        Assert.IsType<TableConstraint>(table.Elements[Count.Two]);
        Assert.NotNull(table.Postgres!.PartitionBy);
        SqlAssert.ParseFails(Sql);
    }

    // ---- DROP ----

    [Fact]
    public void Parses_drop_table_list_if_exists()
    {
        const string Sql = "drop table if exists a, s.b cascade";
        var drop = ParseWhole<DropTableStatement>(Sql);
        Assert.NotNull(drop.Postgres!.IfExists);
        Assert.Equal(Count.Two, Assert.Single(drop.Postgres!.AdditionalNames).Count);
        Assert.Equal("cascade", Text(Sql, drop.Behavior));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_drop_without_behavior()
    {
        const string Sql = "drop table a";
        Assert.Null(ParseWhole<DropTableStatement>(Sql).Behavior);
        SqlAssert.ParseFails(Sql);
        Assert.Null(ParseWhole<DropViewStatement>("drop view v").Behavior);
        Assert.Null(ParseWhole<DropSequenceStatement>("drop sequence s").Behavior);
    }

    [Fact]
    public void Iso_drop_still_requires_behavior()
    {
        var drop = SqlAssert.Parse<DropTableStatement>("drop table a restrict");
        Assert.NotNull(drop.Behavior);
        Assert.Null(drop.Postgres);
    }

    [Fact]
    public void Parses_drop_view_and_sequence_lists()
    {
        const string Views = "drop view if exists v, w restrict";
        var view = ParseWhole<DropViewStatement>(Views);
        Assert.Single(view.Postgres!.AdditionalNames);
        Assert.NotNull(view.Postgres!.IfExists);
        SqlAssert.ParseFails(Views);
        const string Sequences = "drop sequence if exists s, u cascade";
        var sequence = ParseWhole<DropSequenceStatement>(Sequences);
        Assert.Single(sequence.Postgres!.AdditionalNames);
        SqlAssert.ParseFails(Sequences);
    }

    // ---- Visitor ----

    private const string VisitorTableSql =
        "create table p1 partition of t (a default d1) for values from (f1) to (f2) partition by range ((e1 + 1))";

    [Fact]
    public void Visitor_reaches_partition_expressions()
    {
        var visitor = new IdentifierCollector(VisitorTableSql);
        visitor.Visit(SqlAssert.ParsePg<CreateTableStatement>(VisitorTableSql));
        Assert.Equal(["d1", "f1", "f2", "e1"], visitor.Names);
    }

    private sealed class IdentifierCollector(string source) : SqlVisitor
    {
        private readonly List<string> _names = [];

        public IReadOnlyList<string> Names => _names;

        public override void VisitIdentifierExpression(IdentifierExpression node) =>
            _names.Add(source[node.Span.Position..node.Span.End]);
    }
}
