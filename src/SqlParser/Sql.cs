namespace SqlParser;

public static class Sql
{
    public static string Format(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var tokens = Lexer.Lex(sql);
        var statement = Parser.Parse(tokens);
        return Formatter.Format(statement);
    }
}
