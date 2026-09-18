namespace QlParse;

public static class Sql
{
    public static SqlLexResult Lex(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return Lexer.LexAll(sql);
    }

    public static SqlSyntaxTree Parse(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return Parse(Lex(sql));
    }

    public static SqlSyntaxTree Parse(SqlLexResult lexed)
    {
        ArgumentNullException.ThrowIfNull(lexed);
        if (lexed.Error is not null)
        {
            throw lexed.Error;
        }

        return new SqlSyntaxTree
        {
            Source = lexed.Source,
            Root = Parser.Parse(lexed.Tokens, lexed.Source),
            Tokens = lexed.Tokens,
            Trivia = lexed.Trivia,
        };
    }
}
