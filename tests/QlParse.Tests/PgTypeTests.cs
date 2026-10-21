namespace QlParse.Tests;

// Postgres data types (POSTGRES-STANDARD.md "Data types") and generated columns. Every type is checked
// in column definitions, CAST, `::`, CREATE DOMAIN and function RETURNS so its name stays recoverable.
public sealed class PgTypeTests
{
    private const string GeneratedStored = "create table t (a int, b int generated always as (a * 2) stored)";
    private const string GeneratedVirtual = "create table t (a int, b int generated always as (a * 2) virtual)";
    private const string ArrayColumns = "create table t (a int[], b text[][])";

    [Theory]
    [InlineData("serial")]
    [InlineData("smallserial")]
    [InlineData("bigserial")]
    [InlineData("serial2")]
    [InlineData("serial4")]
    [InlineData("serial8")]
    public void Parses_serial_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("jsonb")]
    [InlineData("hstore")]
    [InlineData("uuid")]
    [InlineData("money")]
    [InlineData("tsvector")]
    [InlineData("tsquery")]
    [InlineData("pg_lsn")]
    [InlineData("txid_snapshot")]
    [InlineData("pg_snapshot")]
    public void Parses_document_and_misc_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("point")]
    [InlineData("line")]
    [InlineData("lseg")]
    [InlineData("box")]
    [InlineData("path")]
    [InlineData("polygon")]
    [InlineData("circle")]
    public void Parses_geometric_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("cidr")]
    [InlineData("inet")]
    [InlineData("macaddr")]
    [InlineData("macaddr8")]
    public void Parses_network_address_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("int4range")]
    [InlineData("int8range")]
    [InlineData("numrange")]
    [InlineData("tsrange")]
    [InlineData("tstzrange")]
    [InlineData("daterange")]
    public void Parses_range_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("int4multirange")]
    [InlineData("int8multirange")]
    [InlineData("nummultirange")]
    [InlineData("tsmultirange")]
    [InlineData("tstzmultirange")]
    [InlineData("datemultirange")]
    public void Parses_multirange_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("anyelement")]
    [InlineData("anyarray")]
    [InlineData("anyrange")]
    [InlineData("anymultirange")]
    [InlineData("anynonarray")]
    [InlineData("anyenum")]
    [InlineData("anycompatible")]
    [InlineData("anycompatiblearray")]
    [InlineData("void")]
    [InlineData("record")]
    [InlineData("cstring")]
    [InlineData("internal")]
    [InlineData("trigger")]
    [InlineData("event_trigger")]
    public void Parses_pseudo_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("oid")]
    [InlineData("regclass")]
    [InlineData("regproc")]
    [InlineData("regprocedure")]
    [InlineData("regtype")]
    [InlineData("regnamespace")]
    [InlineData("regrole")]
    [InlineData("regoper")]
    [InlineData("regconfig")]
    [InlineData("regcollation")]
    public void Parses_object_identifier_types(string name)
    {
        AssertNamedType(name);
    }

    [Theory]
    [InlineData("int2")]
    [InlineData("int4")]
    [InlineData("int8")]
    [InlineData("float4")]
    [InlineData("float8")]
    [InlineData("bool")]
    [InlineData("text")]
    [InlineData("bytea")]
    [InlineData("citext")]
    [InlineData("timestamptz")]
    [InlineData("timetz")]
    [InlineData("varbit")]
    [InlineData("xml")]
    [InlineData("interval")]
    [InlineData("name")]
    public void Parses_postgres_type_spellings(string spelling)
    {
        AssertNamedType(spelling);
    }

    [Theory]
    [InlineData("bit varying(10)")]
    [InlineData("character varying(10)")]
    [InlineData("double precision")]
    [InlineData("timestamp(3) with time zone")]
    [InlineData("time(6) without time zone")]
    [InlineData("timestamp without time zone")]
    [InlineData("numeric(10,2)")]
    [InlineData("interval(3)")]
    [InlineData("interval day to second(3)")]
    public void Parses_multi_word_type_spellings(string type)
    {
        Assert.Equal(type, Text($"create table t (a {type})", ColumnType(type).Span));
        var cast = SqlAssert.ExprPg<CastExpression>($"cast(x as {type})");
        Assert.Equal(type, Text($"select cast(x as {type})", cast.Type.Span));
        var colon = SqlAssert.ExprPg<ColonCastExpression>($"x::{type}");
        Assert.Equal(type, Text($"select x::{type}", colon.Type.Span));
    }

    [Fact]
    public void Parses_bit_varying()
    {
        var type = ColumnType("bit varying(10)");
        Assert.Equal("varying", Text("create table t (a bit varying(10))", Assert.Single(type.NameTail).Span));
        Assert.NotNull(type.Precision);
        SqlAssert.ParseFails("create table t (a bit varying(10))");
    }

    [Fact]
    public void Parses_without_time_zone()
    {
        const string Sql = "create table t (a time(6) without time zone)";
        var type = ColumnType("time(6) without time zone");
        Assert.Equal("without", Text(Sql, Assert.NotNull(type.WithKeyword).Span));
        Assert.NotNull(type.Zone);
        Assert.NotNull(type.Precision);
        SqlAssert.ParseFails(Sql);
        SqlAssert.ParseFails("select cast(x as timestamp without time zone)");
    }

    [Theory]
    [InlineData("interval year")]
    [InlineData("interval month")]
    [InlineData("interval day")]
    [InlineData("interval hour")]
    [InlineData("interval minute")]
    [InlineData("interval second")]
    [InlineData("interval second(3)")]
    [InlineData("interval year to month")]
    [InlineData("interval day to hour")]
    [InlineData("interval day to minute")]
    [InlineData("interval day to second")]
    [InlineData("interval hour to minute")]
    [InlineData("interval hour to second")]
    [InlineData("interval minute to second")]
    public void Parses_interval_field_restrictions(string type)
    {
        var dataType = ColumnType(type);
        Assert.NotNull(dataType.IntervalQualifier);
        Assert.Equal(type, Text($"create table t (a {type})", dataType.Span));
        SqlAssert.ParseFails($"create table t (a {type})");
    }

    [Fact]
    public void Parses_interval_qualifier_parts()
    {
        const string Sql = "select x::interval day to second(3)";
        var type = SqlAssert.ExprPg<ColonCastExpression>("x::interval day to second(3)").Type;
        var qualifier = type.IntervalQualifier!;
        Assert.Equal("day", Text(Sql, qualifier.Start.Name.Span));
        Assert.Equal("second(3)", Text(Sql, qualifier.End!.Span));
        Assert.NotNull(qualifier.End.Precision);
        Assert.Equal("interval day to second(3)", Text(Sql, type.Span));
    }

    [Fact]
    public void Interval_without_fields_leaves_column_tail_alone()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>("create table t (a interval default '1 day', b interval not null)");
        var first = Assert.IsType<ColumnDefinition>(table.Elements![0]);
        Assert.Null(first.Type!.IntervalQualifier);
        Assert.NotNull(first.Default);
        Assert.Single(Assert.IsType<ColumnDefinition>(table.Elements[1]).Constraints);
    }

    [Theory]
    [InlineData("int[]", 1)]
    [InlineData("text[]", 1)]
    [InlineData("int[3]", 1)]
    [InlineData("int[][]", Count.Two)]
    [InlineData("int[3][4]", Count.Two)]
    [InlineData("varchar(10)[]", 1)]
    [InlineData("character varying(3)[]", 1)]
    [InlineData("bit varying(3)[]", 1)]
    [InlineData("timestamp with time zone[]", 1)]
    [InlineData("interval day[]", 1)]
    [InlineData("s.t[]", 1)]
    [InlineData("jsonb[][][]", Count.Three)]
    public void Parses_array_types(string type, int dimensions)
    {
        var dataType = ColumnType(type);
        Assert.Equal(dimensions, dataType.ArrayBounds.Count);
        Assert.Empty(dataType.Collections);
        Assert.Equal(type, Text($"create table t (a {type})", dataType.Span));
        SqlAssert.ParseFails($"create table t (a {type})");
    }

    [Fact]
    public void Parses_array_bound_sizes()
    {
        const string Sql = "create table t (a int[3][])";
        var bounds = ColumnType("int[3][]").ArrayBounds;
        Assert.Equal(Count.Two, bounds.Count);
        Assert.Equal("[3]", Text(Sql, bounds[0].Span));
        Assert.Equal("3", Text(Sql, Assert.NotNull(bounds[0].Size).Span));
        Assert.Null(bounds[1].Size);
        Assert.Equal(SyntaxKind.OpenBracket, bounds[1].OpenBracket.Kind);
        Assert.Equal(SyntaxKind.CloseBracket, bounds[1].CloseBracket.Kind);
    }

    [Fact]
    public void Parses_multiple_array_columns()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>(ArrayColumns);
        Assert.Single(Assert.IsType<ColumnDefinition>(table.Elements![0]).Type!.ArrayBounds);
        Assert.Equal(Count.Two, Assert.IsType<ColumnDefinition>(table.Elements[1]).Type!.ArrayBounds.Count);
        SqlAssert.ParseFails(ArrayColumns);
    }

    [Fact]
    public void Parses_array_colon_cast()
    {
        const string Cast = "'{1,2}'::int[]";
        const string Sql = $"select {Cast}";
        var cast = SqlAssert.ExprPg<ColonCastExpression>(Cast);
        Assert.IsType<LiteralExpression>(cast.Expression);
        Assert.Single(cast.Type.ArrayBounds);
        Assert.Equal(Cast, Text(Sql, cast.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_array_cast_and_domain()
    {
        const string Domain = "create domain d as text[]";
        Assert.Single(SqlAssert.ExprPg<CastExpression>("cast(x as int[])").Type.ArrayBounds);
        var domain = SqlAssert.ParsePg<CreateDomainStatement>(Domain);
        Assert.Equal("text[]", Text(Domain, domain.Type.Span));
        SqlAssert.ParseFails("select cast(x as int[])");
        SqlAssert.ParseFails(Domain);
    }

    [Fact]
    public void Parses_array_routine_parameter_and_returns()
    {
        const string Sql = "create function f(x int[]) returns int[] sql return x";
        var function = SqlAssert.ParsePg<CreateRoutineStatement>(Sql);
        Assert.Single(Assert.Single(function.Parameters).Type.ArrayBounds);
        Assert.Equal("int[]", Text(Sql, function.ReturnsType!.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_array_add_column()
    {
        const string Sql = "alter table t add column a int[]";
        var alter = SqlAssert.ParsePg<AlterTableStatement>(Sql);
        var column = Assert.IsType<AddColumnAction>(alter.Action).Column;
        Assert.Equal("a int[]", Text(Sql, column.Span));
        SqlAssert.ParseFails(Sql);
    }

    [Fact]
    public void Parses_iso_array_keyword_under_postgres()
    {
        const string Sql = "create table t (a int array, b int array[4])";
        var table = SqlAssert.ParsePg<CreateTableStatement>(Sql);
        var plain = Assert.IsType<ColumnDefinition>(table.Elements![0]).Type!;
        Assert.Equal(SyntaxKind.ArrayKeyword, Assert.Single(plain.Collections).Keyword.Kind);
        Assert.Empty(plain.ArrayBounds);
        var bounded = Assert.IsType<ColumnDefinition>(table.Elements[1]).Type!;
        Assert.Equal("int array[4]", Text(Sql, bounded.Span));
        Assert.NotNull(Assert.Single(bounded.Collections).Cardinality);
    }

    [Fact]
    public void Visits_array_bounds_and_interval_qualifier()
    {
        var visitor = new TypePartCounter();
        visitor.Visit(SqlAssert.ParsePg<CreateTableStatement>("create table t (a int[3][], b interval day to second)"));
        Assert.Equal(Count.Two, visitor.ArrayBounds);
        Assert.Equal(1, visitor.IntervalQualifiers);
    }

    [Fact]
    public void Parses_generated_stored_column()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>(GeneratedStored);
        var column = Assert.IsType<ColumnDefinition>(table.Elements![1]);
        var generated = column.Generated!;
        Assert.IsType<BinaryExpression>(generated.Expression);
        Assert.Equal("stored", Text(GeneratedStored, Assert.NotNull(generated.StorageKeyword).Span));
        Assert.Equal("generated always as (a * 2) stored", Text(GeneratedStored, generated.Span));
        Assert.Equal("b int generated always as (a * 2) stored", Text(GeneratedStored, column.Span));
        SqlAssert.ParseFails(GeneratedStored);
    }

    [Fact]
    public void Parses_generated_virtual_column()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>(GeneratedVirtual);
        var generated = Assert.IsType<ColumnDefinition>(table.Elements![1]).Generated!;
        Assert.Equal("virtual", Text(GeneratedVirtual, Assert.NotNull(generated.StorageKeyword).Span));
        Assert.Equal("generated always as (a * 2) virtual", Text(GeneratedVirtual, generated.Span));
        SqlAssert.ParseFails(GeneratedVirtual);
    }

    [Fact]
    public void Parses_generated_column_followed_by_constraint()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>("create table t (a int, b int generated always as (a + 1) stored not null)");
        var column = Assert.IsType<ColumnDefinition>(table.Elements![1]);
        Assert.NotNull(column.Generated!.StorageKeyword);
        Assert.Single(column.Constraints);
    }

    [Fact]
    public void Parses_generated_column_without_storage_keyword()
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>("create table t (a int, b int generated always as (a + 1))");
        var generated = Assert.IsType<ColumnDefinition>(table.Elements![1]).Generated!;
        Assert.Null(generated.StorageKeyword);
        Assert.NotNull(generated.CloseParen);
    }

    [Theory]
    [InlineData("create table t (a int generated always as identity)")]
    [InlineData("create table t (a int generated by default as identity)")]
    [InlineData("create table t (a int generated by default as identity (start with 1 increment by 1))")]
    public void Parses_identity_columns_under_postgres(string sql)
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>(sql);
        var column = Assert.IsType<ColumnDefinition>(Assert.Single(table.Elements!));
        Assert.NotNull(column.Identity);
        Assert.Null(column.Generated);
        Assert.StartsWith("generated", Text(sql, column.Identity.Span), StringComparison.Ordinal);
        Assert.Equal(column.Span.End, column.Identity.Span.End);
    }

    [Fact]
    public void Parses_column_collate()
    {
        const string Sql = """create table t (a text collate "C")""";
        var column = Assert.IsType<ColumnDefinition>(Assert.Single(SqlAssert.ParsePg<CreateTableStatement>(Sql).Elements!));
        Assert.NotNull(column.CollateKeyword);
        var collation = Assert.Single(column.Collation!);
        Assert.Equal(column.Span.End, collation.Span.End);
        Assert.Equal(Count.Three, collation.Length);
        Assert.Equal("C", Text(Sql, collation.Span).Trim('"'));
    }

    [Fact]
    public void Parses_default_nextval_regclass()
    {
        const string Sql = "create table t (a int default nextval('s'::regclass))";
        var column = Assert.IsType<ColumnDefinition>(Assert.Single(SqlAssert.ParsePg<CreateTableStatement>(Sql).Elements!));
        var call = Assert.IsType<FunctionCallExpression>(column.Default!.Value);
        var cast = Assert.IsType<ColonCastExpression>(Assert.Single(call.Arguments));
        Assert.Equal("regclass", Text(Sql, cast.Type.Name.Span));
        Assert.Equal("default nextval('s'::regclass)", Text(Sql, column.Default.Span));
    }

    private static void AssertNamedType(string name)
    {
        var columnSql = $"create table t (a {name})";
        var column = ColumnType(name);
        Assert.Equal(name, Text(columnSql, column.Name.Span));
        Assert.Equal(name, Text(columnSql, column.Span));
        Assert.Empty(column.ArrayBounds);

        var castSql = $"select cast(x as {name})";
        Assert.Equal(name, Text(castSql, SqlAssert.ExprPg<CastExpression>($"cast(x as {name})").Type.Name.Span));

        var colonSql = $"select x::{name}";
        Assert.Equal(name, Text(colonSql, SqlAssert.ExprPg<ColonCastExpression>($"x::{name}").Type.Span));

        var domainSql = $"create domain d as {name}";
        Assert.Equal(name, Text(domainSql, SqlAssert.ParsePg<CreateDomainStatement>(domainSql).Type.Span));

        var routineSql = $"create function f() returns {name} sql return null";
        Assert.Equal(name, Text(routineSql, SqlAssert.ParsePg<CreateRoutineStatement>(routineSql).ReturnsType!.Span));

        var arraySql = $"create table t (a {name}[])";
        Assert.Equal($"{name}[]", Text(arraySql, ColumnType($"{name}[]").Span));
    }

    private static DataType ColumnType(string type)
    {
        var table = SqlAssert.ParsePg<CreateTableStatement>($"create table t (a {type})");
        return Assert.IsType<ColumnDefinition>(Assert.Single(table.Elements!)).Type!;
    }

    private static string Text(string source, SourceSpan span) =>
        source[span.Position..span.End];

    private sealed class TypePartCounter : SqlVisitor
    {
        public int ArrayBounds { get; private set; }

        public int IntervalQualifiers { get; private set; }

        public override void VisitArrayBound(ArrayBound node)
        {
            ArrayBounds++;
            base.VisitArrayBound(node);
        }

        public override void VisitIntervalQualifier(IntervalQualifier node)
        {
            IntervalQualifiers++;
            base.VisitIntervalQualifier(node);
        }
    }
}
