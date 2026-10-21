namespace QlParse.Tests;

internal static class SqlAssert
{
    public static SqlLexResult Lex(string sql)
    {
        var result = Sql.Lex(sql);
        Assert.Null(result.Error);
        return result;
    }

    public static SyntaxKind[] Kinds(string sql) =>
        [.. Lex(sql).Tokens.Select(token => token.Kind)];

    public static T Parse<T>(string sql) where T : Query
    {
        var result = Sql.Parse(sql);
        Assert.Null(result.Error);
        return Assert.IsType<T>(result.Root);
    }

    public static SelectStatement Select(string sql) =>
        Parse<SelectStatement>(sql);

    public static Expression Expr(string sql) =>
        Select("select " + sql).SelectList[0].Expression;

    public static T Expr<T>(string sql) where T : Expression =>
        Assert.IsType<T>(Expr(sql));

    public static T ParsePg<T>(string sql) where T : Query
    {
        var result = Sql.Parse(sql, SqlOptions.Postgres);
        Assert.Null(result.Error);
        return Assert.IsType<T>(result.Root);
    }

    public static SelectStatement SelectPg(string sql) =>
        ParsePg<SelectStatement>(sql);

    public static Expression ExprPg(string sql) =>
        SelectPg("select " + sql).SelectList[0].Expression;

    public static T ExprPg<T>(string sql) where T : Expression =>
        Assert.IsType<T>(ExprPg(sql));

    public static T ParseTsql<T>(string sql) where T : Query
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        Assert.Null(result.Error);
        return Assert.IsType<T>(result.Root);
    }

    public static SelectStatement SelectTsql(string sql) =>
        ParseTsql<SelectStatement>(sql);

    public static Expression ExprTsql(string sql) =>
        SelectTsql("select " + sql).SelectList[0].Expression;

    public static T ExprTsql<T>(string sql) where T : Expression =>
        Assert.IsType<T>(ExprTsql(sql));

    public static void ParseFails(string sql, SqlOptions options = SqlOptions.None) =>
        Assert.NotNull(Sql.Parse(sql, options).Error);
}
