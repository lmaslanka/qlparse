namespace QlParse.Tests;

// SQL Server variables and cursors (stream E): DECLARE (scalar, TABLE and CURSOR variables), SET @v,
// compound SET, T-SQL cursor declarations, SET @c = CURSOR and OPEN / FETCH / CLOSE / DEALLOCATE.
public sealed partial class TsqlProcTests
{
    // ---- DECLARE ----

    [Fact]
    public void Parses_declare_with_several_variables_and_initializers()
    {
        const string sql = "declare @a int = 1, @b as nvarchar(50), @c decimal(10, 2) = @a * 2";
        var statement = SqlAssert.ParseTsql<TsqlDeclareStatement>(sql);
        Assert.Equal(Three, statement.Variables.Count);
        var first = statement.Variables[0];
        Assert.Equal(VariableA, Text(sql, first.Name));
        Assert.Equal("int", Text(sql, Present(first.DataType).Name));
        Assert.Equal("1", Text(sql, Present(first.Value).Span));
        var second = statement.Variables[1];
        Assert.NotNull(second.AsKeyword);
        Assert.Null(second.Value);
        Assert.IsType<BinaryExpression>(statement.Variables[Two].Value);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_table_variable_declaration()
    {
        const string sql = "declare @t table (id int not null primary key, name varchar(20), unique (name))";
        var variable = Assert.Single(SqlAssert.ParseTsql<TsqlDeclareStatement>(sql).Variables);
        var table = Present(variable.Table);
        Assert.Null(variable.DataType);
        Assert.Equal("table", Text(sql, table.TableKeyword));
        Assert.Equal(Three, table.Elements.Count);
        Assert.IsType<ColumnDefinition>(table.Elements[0]);
        Assert.Equal(sql, Text(sql, SqlAssert.ParseTsql<TsqlDeclareStatement>(sql).Span));
    }

    [Fact]
    public void Parses_cursor_variable_declaration()
    {
        const string sql = "declare @c cursor, @n int";
        var statement = SqlAssert.ParseTsql<TsqlDeclareStatement>(sql);
        Assert.Equal("cursor", Text(sql, Assert.NotNull(statement.Variables[0].CursorKeyword)));
        Assert.Null(statement.Variables[0].DataType);
        Assert.NotNull(statement.Variables[1].DataType);
    }

    [Fact]
    public void Declare_variables_need_a_type_and_the_flag()
    {
        SqlAssert.ParseFails("declare @a", SqlOptions.SqlServer);
        SqlAssert.ParseFails("declare @a = 1", SqlOptions.SqlServer);
        FailsInIso("declare @a int");
    }

    // ---- SET @v ----

    [Fact]
    public void Parses_set_variable()
    {
        const string sql = "set @a = (select max(id) from t)";
        var statement = SqlAssert.ParseTsql<TsqlSetVariableStatement>(sql);
        Assert.Equal(VariableA, Text(sql, statement.Variable));
        Assert.Equal("=", Text(sql, statement.OperatorToken));
        Assert.NotNull(statement.Value);
        Assert.Null(statement.Cursor);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Theory]
    [InlineData("set @a += 1", "+=")]
    [InlineData("set @a -= 1", "-=")]
    [InlineData("set @a *= 2", "*=")]
    [InlineData("set @a /= 2", "/=")]
    [InlineData("set @a %= 2", "%=")]
    [InlineData("set @a &= 2", "&=")]
    [InlineData("set @a |= 2", "|=")]
    [InlineData("set @a ^= 2", "^=")]
    public void Parses_compound_set(string sql, string op)
    {
        var statement = SqlAssert.ParseTsql<TsqlSetVariableStatement>(sql);
        Assert.Equal(op, Text(sql, statement.OperatorToken));
        Assert.IsType<LiteralExpression>(statement.Value);
    }

    [Fact]
    public void Set_variable_requires_an_assignment()
    {
        SqlAssert.ParseFails("set @a 1", SqlOptions.SqlServer);
        FailsInIso("set @a += 1");
    }

    [Fact]
    public void Statements_follow_a_set_without_terminator()
    {
        var batch = SqlAssert.ParseTsql<TsqlBatch>("set @a = 1 set @b = @a select @b");
        Assert.Collection(
            batch.Statements,
            statement => Assert.IsType<TsqlSetVariableStatement>(statement),
            statement => Assert.IsType<TsqlSetVariableStatement>(statement),
            statement => Assert.IsType<SelectStatement>(statement));
    }

    // ---- Cursors ----

    [Fact]
    public void Parses_tsql_cursor_declaration()
    {
        const string sql = "declare c cursor local forward_only static read_only type_warning for select a from t";
        var statement = SqlAssert.ParseTsql<TsqlDeclareCursorStatement>(sql);
        Assert.Equal(CursorC, Text(sql, statement.Name));
        Assert.Equal(["local", "forward_only", "static", "read_only", "type_warning"], Texts(sql, statement.Cursor.Options));
        Assert.IsType<SelectStatement>(statement.Cursor.Query);
        Assert.Equal(sql, Text(sql, statement.Span));
    }

    [Fact]
    public void Parses_cursor_for_update_of_columns()
    {
        const string sql = "declare c cursor global scroll keyset scroll_locks for select a, b from t for update of a";
        var statement = SqlAssert.ParseTsql<TsqlDeclareCursorStatement>(sql);
        var query = Assert.IsType<SelectStatement>(statement.Cursor.Query);
        var lockClause = Present(query.Lock);
        Assert.Equal(["a"], Texts(sql, Present(lockClause.Columns)));
    }

    [Fact]
    public void Iso_cursor_form_stays_an_iso_declaration()
    {
        var statement = SqlAssert.ParseTsql<DeclareCursorStatement>("declare c insensitive scroll cursor for select a from t for read only");
        Assert.NotNull(statement.Scroll);
        SqlAssert.ParseTsql<DeclareCursorStatement>("declare c cursor for select 1");
        FailsInIso("declare c cursor local for select 1");
    }

    [Fact]
    public void Parses_set_cursor_variable()
    {
        const string sql = "set @c = cursor fast_forward for select a from t";
        var statement = SqlAssert.ParseTsql<TsqlSetVariableStatement>(sql);
        var cursor = Present(statement.Cursor);
        Assert.Null(statement.Value);
        Assert.Equal(["fast_forward"], Texts(sql, cursor.Options));
        Assert.Equal(sql, Text(sql, statement.Span));
        Assert.Empty(Present(SqlAssert.ParseTsql<TsqlSetVariableStatement>("set @c = cursor for select 1").Cursor).Options);
        SqlAssert.ParseFails("set @c += cursor for select 1", SqlOptions.SqlServer);
    }

    [Fact]
    public void Parses_open_fetch_close_deallocate()
    {
        const string sql = "open c fetch next from c into @a, @b close global c deallocate @cv";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        var open = Assert.IsType<TsqlCursorStatement>(batch.Statements[0]);
        Assert.Equal("open", Text(sql, open.VerbKeyword));
        Assert.Equal(CursorC, Text(sql, open.Cursor));
        var fetch = Assert.IsType<TsqlFetchStatement>(batch.Statements[1]);
        Assert.Equal("next", Text(sql, Assert.NotNull(fetch.Orientation)));
        Assert.Equal(["@a", "@b"], Texts(sql, fetch.Variables));
        var close = Assert.IsType<TsqlCursorStatement>(batch.Statements[Two]);
        Assert.Equal("global", Text(sql, Assert.NotNull(close.GlobalKeyword)));
        var deallocate = Assert.IsType<TsqlCursorStatement>(batch.Statements[Three]);
        Assert.Equal("@cv", Text(sql, deallocate.Cursor));
    }

    [Theory]
    [InlineData("fetch prior from c", "prior")]
    [InlineData("fetch first from c", "first")]
    [InlineData("fetch last from global c", "last")]
    public void Parses_fetch_orientations(string sql, string orientation)
    {
        var fetch = SqlAssert.ParseTsql<TsqlFetchStatement>(sql);
        Assert.Equal(orientation, Text(sql, Assert.NotNull(fetch.Orientation)));
        Assert.Equal(CursorC, Text(sql, fetch.Cursor));
        Assert.Equal(sql, Text(sql, fetch.Span));
    }

    [Fact]
    public void Parses_fetch_absolute_relative_and_bare()
    {
        const string sql = "fetch relative -2 from @c into @x";
        var relative = SqlAssert.ParseTsql<TsqlFetchStatement>(sql);
        Assert.Equal("-2", Text(sql, Present(relative.Offset).Span));
        Assert.NotNull(SqlAssert.ParseTsql<TsqlFetchStatement>("fetch absolute @n from c").Offset);
        var bare = SqlAssert.ParseTsql<TsqlFetchStatement>("fetch c");
        Assert.Null(bare.Orientation);
        Assert.Null(bare.FromKeyword);
        SqlAssert.ParseFails("fetch absolute 1 c", SqlOptions.SqlServer);
    }

    [Fact]
    public void Open_and_close_of_keys_are_not_cursor_statements()
    {
        SqlAssert.ParseTsql<TsqlSecurityObjectStatement>("open symmetric key k decryption by password = 'x'");
        SqlAssert.ParseTsql<TsqlSecurityObjectStatement>("open master key decryption by password = 'p'");
        SqlAssert.ParseTsql<TsqlSecurityObjectStatement>("close symmetric key k");
        SqlAssert.ParseTsql<TsqlSecurityObjectStatement>("close master key");
        SqlAssert.ParseTsql<TsqlSecurityObjectStatement>("close all symmetric keys");
        const string sql = "open key_cursor close key_cursor";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal("key_cursor", Text(sql, Assert.IsType<TsqlCursorStatement>(batch.Statements[1]).Cursor));
        SqlAssert.Parse<OpenStatement>("open c");
    }

    // ---- Table variables in DML ----

    [Fact]
    public void Table_variables_are_table_names()
    {
        const string sql = "insert into @t values (1) update @t set a = 2 delete from @t select * from @t as x";
        var batch = SqlAssert.ParseTsql<TsqlBatch>(sql);
        Assert.Equal(["@t"], Texts(sql, Assert.IsType<InsertStatement>(batch.Statements[0]).TableName));
        Assert.Equal(["@t"], Texts(sql, Assert.IsType<UpdateStatement>(batch.Statements[1]).Target.NameParts));
        Assert.Equal(["@t"], Texts(sql, Assert.IsType<DeleteStatement>(batch.Statements[Two]).Target.NameParts));
        var table = Assert.IsType<TableReference>(Assert.IsType<SelectStatement>(batch.Statements[Three]).From);
        Assert.Equal(["@t"], Texts(sql, table.NameParts));
        FailsInIso("select * from @t");
    }
}
