namespace QlParse.Tests;

// SQL Server data types (SQLSERVER-STANDARD.md "Data types"). The type grammar is shared, so every type is
// checked in CAST and in a column definition, and the T-SQL-only shapes in CONVERT / TRY_CAST as well.
public sealed partial class TsqlExpressionTests
{
    private const string TypedXmlColumns = "create table t (a xml(content dbo.sc), b xml(document sc), c xml(db.dbo.sc))";
    private const string VectorType = "vector(3, float16)";
    private const string TypedXml = "xml(content dbo.sc)";

    private static string CastOf(string type) => $"cast(a as {type})";

    private static DataType CastType(string type) => SqlAssert.ExprTsql<CastExpression>(CastOf(type)).Type;

    private static DataType ColumnType(string type)
    {
        var table = SqlAssert.ParseTsql<CreateTableStatement>($"create table t (c {type})");
        var column = Assert.IsType<ColumnDefinition>(Assert.Single(table.Elements!));
        return Assert.IsType<DataType>(column.Type);
    }

    private static string CastText(string type, SourceSpan span) => ExprText(CastOf(type), span);

    [Theory]
    [InlineData("nchar", "nchar(10)")]
    [InlineData("nvarchar", "nvarchar(4000)")]
    [InlineData("text", "text")]
    [InlineData("ntext", "ntext")]
    [InlineData("image", "image")]
    [InlineData("tinyint", "tinyint")]
    [InlineData("bit", "bit")]
    [InlineData("money", "money")]
    [InlineData("smallmoney", "smallmoney")]
    [InlineData("datetime", "datetime")]
    [InlineData("smalldatetime", "smalldatetime")]
    [InlineData("datetime2", "datetime2(7)")]
    [InlineData("datetimeoffset", "datetimeoffset(3)")]
    [InlineData("uniqueidentifier", "uniqueidentifier")]
    [InlineData("rowversion", "rowversion")]
    [InlineData("sql_variant", "sql_variant")]
    [InlineData("xml", "xml")]
    [InlineData("hierarchyid", "hierarchyid")]
    [InlineData("geometry", "geometry")]
    [InlineData("geography", "geography")]
    [InlineData("vector", "vector(1536)")]
    [InlineData("json", "json")]
    public void Parses_sql_server_types_as_named_types(string name, string type)
    {
        var cast = CastType(type);
        Assert.Equal(type, CastText(type, cast.Span));
        Assert.Equal(name, CastText(type, cast.Name.Span));
        foreach (var dataType in new[] { cast, ColumnType(type) })
        {
            Assert.Equal(SyntaxKind.Identifier, dataType.Name.Kind);
            Assert.Equal(type.Length, dataType.Span.Length);
            Assert.Null(dataType.SqlServer);
        }
    }

    [Fact]
    public void Timestamp_is_the_row_version_type_without_a_time_zone()
    {
        var dataType = ColumnType("timestamp not null");
        Assert.Equal(SyntaxKind.Identifier, dataType.Name.Kind);
        Assert.Null(dataType.OpenParen);
        Assert.Null(dataType.WithKeyword);
    }

    [Fact]
    public void Precision_carries_the_length_of_sql_server_types()
    {
        const string type = "datetimeoffset(3)";
        var dataType = CastType(type);
        Assert.Equal("3", CastText(type, Assert.NotNull(dataType.Precision).Span));
        Assert.NotNull(CastType("decimal(10, 2)").Scale);
    }

    [Theory]
    [InlineData("varchar")]
    [InlineData("nvarchar")]
    [InlineData("varbinary")]
    public void Max_length_takes_the_place_of_the_precision(string name)
    {
        var type = $"{name}(max)";
        Assert.Equal("max", CastText(type, Assert.NotNull(CastType(type).Precision).Span));
        foreach (var dataType in new[] { CastType(type), ColumnType(type), CastType($"{name}(MAX)") })
        {
            var precision = Assert.NotNull(dataType.Precision);
            Assert.Equal(SyntaxKind.Identifier, precision.Kind);
            Assert.NotNull(dataType.CloseParen);
            Assert.Null(dataType.Scale);
        }

        SqlAssert.ParseFails($"{SelectPrefix}{CastOf(type)}");
        SqlAssert.ParseFails($"{SelectPrefix}{CastOf(type)}", SqlOptions.Postgres);
    }

    [Theory]
    [InlineData("varchar(min)")]
    [InlineData("decimal(10, max)")]
    [InlineData("vector(3, int)")]
    public void Only_max_and_vector_element_types_replace_a_number(string type)
    {
        SqlAssert.ParseFails($"{SelectPrefix}{CastOf(type)}", SqlOptions.SqlServer);
    }

    [Fact]
    public void Vector_takes_an_optional_element_type()
    {
        var dataType = CastType(VectorType);
        Assert.Equal("float16", CastText(VectorType, Assert.NotNull(dataType.Scale).Span));
        Assert.Equal(SyntaxKind.Identifier, Assert.NotNull(dataType.Scale).Kind);
        Assert.Equal(SyntaxKind.Identifier, Assert.NotNull(ColumnType("vector(3, float32) not null").Scale).Kind);
        SqlAssert.ParseFails($"{SelectPrefix}{CastOf(VectorType)}");
    }

    [Fact]
    public void Typed_xml_names_its_schema_collection()
    {
        var dataType = CastType(TypedXml);
        Assert.Equal("content", CastText(TypedXml, Assert.NotNull(dataType.Modifier).Span));
        var extras = Assert.IsType<TsqlDataTypeExtras>(dataType.SqlServer);
        Assert.Equal(["dbo", "sc"], extras.SchemaCollection.Select(part => CastText(TypedXml, part.Span)));
        Assert.Null(dataType.Postgres);
        Assert.Equal(TypedXml, CastText(TypedXml, dataType.Span));

        var untyped = CastType("xml(sc)");
        Assert.Null(untyped.Modifier);
        Assert.Single(Assert.IsType<TsqlDataTypeExtras>(untyped.SqlServer).SchemaCollection);

        var collectionNamedContent = CastType("xml(content)");
        Assert.Null(collectionNamedContent.Modifier);
        Assert.Single(collectionNamedContent.SqlServer!.SchemaCollection);
    }

    [Fact]
    public void Typed_xml_columns_keep_modifier_and_collection()
    {
        var table = SqlAssert.ParseTsql<CreateTableStatement>(TypedXmlColumns);
        var types = table.Elements!.Select(element => Assert.IsType<ColumnDefinition>(element).Type!).ToList();
        Assert.All(types, type => Assert.NotNull(type.SqlServer));
        Assert.Equal([true, true, false], types.Select(type => type.Modifier is not null));
        Assert.Equal(3, types[^1].SqlServer!.SchemaCollection.Count);
        SqlAssert.ParseFails(TypedXmlColumns);
    }

    [Fact]
    public void Iso_xml_modifier_type_is_unchanged_without_sql_server()
    {
        var dataType = SqlAssert.Expr<CastExpression>("cast(a as xml(document))").Type;
        Assert.NotNull(dataType.Modifier);
        Assert.Null(dataType.SqlServer);
        Assert.Null(dataType.Postgres);
        SqlAssert.ParseFails($"{SelectPrefix}{CastOf(TypedXml)}");
    }

    [Fact]
    public void Sql_server_types_work_in_conversions_and_column_changes()
    {
        Assert.NotNull(SqlAssert.ExprTsql<TsqlConvertExpression>("convert(nvarchar(max), a)").Type.Precision);
        Assert.NotNull(SqlAssert.ExprTsql<TsqlCastExpression>("try_cast(a as xml(dbo.sc))").Type.SqlServer);
        SqlAssert.ParseTsql<AlterTableStatement>("alter table t add c varbinary(max)");
    }
}
