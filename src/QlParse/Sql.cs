namespace QlParse;

public static class Sql
{
    public static SqlLexResult Lex(string sql, SqlFlags flags = SqlFlags.None)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return Lexer.LexAll(sql, flags);
    }

    public static SqlParseResult Parse(string sql, SqlFlags flags = SqlFlags.None)
    {
        ArgumentNullException.ThrowIfNull(sql);
        return Parse(Lex(sql, flags));
    }

    public static SqlParseResult Parse(SqlLexResult lexed)
    {
        ArgumentNullException.ThrowIfNull(lexed);
        if (lexed.Error is not null)
        {
            return Fail(lexed, lexed.Error);
        }

        try
        {
            return new SqlParseResult
            {
                Source = lexed.Source,
                Root = Parser.Parse(lexed.Tokens, lexed.Source),
                Tokens = lexed.Tokens,
                Trivia = lexed.Trivia,
                Error = null,
            };
        }
        catch (SqlParseException ex)
        {
            return Fail(lexed, ex);
        }
    }

    private static SqlParseResult Fail(SqlLexResult lexed, SqlParseException error) =>
        new()
        {
            Source = lexed.Source,
            Root = null,
            Tokens = lexed.Tokens,
            Trivia = lexed.Trivia,
            Error = error,
        };
}
