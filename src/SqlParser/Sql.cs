using System.Diagnostics;

namespace SqlParser;

public static class Sql
{
    public static string Format(string sql, bool color = false)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var lexed = Lexer.LexAll(sql);
        if (lexed.Error is not null)
        {
            throw lexed.Error;
        }

        var statement = Parser.Parse(lexed.Tokens);
        return Formatter.Format(sql, statement, lexed.Trivia, lexed.Tokens[^1], color);
    }

    public static SqlTrace Trace(
        string sql,
        bool dumpLexer = true,
        bool dumpParser = true,
        bool format = true,
        bool stats = true)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var total = stats ? Stopwatch.StartNew() : null;
        var lexerWatch = stats ? Stopwatch.StartNew() : null;
        var lexed = Lexer.LexAll(sql);
        var lexerTime = lexerWatch?.Elapsed ?? TimeSpan.Zero;
        var tokenCount = stats ? CountTokens(lexed.Tokens) : 0;
        var comments = stats ? CountComments(lexed.Trivia) : 0;
        var sourceLines = stats ? CountLines(sql) : 0;

        if (lexed.Error is not null)
        {
            var dumpWatch = stats && dumpLexer ? Stopwatch.StartNew() : null;
            var lexerDump = dumpLexer
                ? StageDumper.DumpTokens(sql, lexed.Tokens, lexed.Trivia)
                : string.Empty;
            return Fail(
                lexerDump,
                lexed.Error,
                stats ? sql.Length : 0,
                sourceLines,
                tokenCount,
                comments,
                lexerTime,
                parserTime: null,
                dumpWatch?.Elapsed,
                total?.Elapsed ?? TimeSpan.Zero,
                SqlStats.LexerStage);
        }

        Query? statement = null;
        SqlParseException? parseError = null;
        TimeSpan? parserTime = null;
        if (dumpParser || format)
        {
            var parserWatch = stats ? Stopwatch.StartNew() : null;
            try
            {
                statement = Parser.Parse(lexed.Tokens);
            }
            catch (SqlParseException ex)
            {
                parseError = ex;
            }

            parserTime = parserWatch?.Elapsed;
            if (parseError is not null || statement is null)
            {
                var dumpWatch = stats && dumpLexer ? Stopwatch.StartNew() : null;
                var lexerDump = dumpLexer
                    ? StageDumper.DumpTokens(sql, lexed.Tokens, lexed.Trivia)
                    : string.Empty;
                return Fail(
                    lexerDump,
                    parseError!,
                    stats ? sql.Length : 0,
                    sourceLines,
                    tokenCount,
                    comments,
                    lexerTime,
                    parserTime,
                    dumpWatch?.Elapsed,
                    total?.Elapsed ?? TimeSpan.Zero,
                    SqlStats.ParserStage);
            }
        }

        string? formatted = null;
        TimeSpan? formatterTime = null;
        if (format && statement is not null)
        {
            var formatWatch = stats ? Stopwatch.StartNew() : null;
            formatted = Formatter.Format(sql, statement, lexed.Trivia, lexed.Tokens[^1]);
            formatterTime = formatWatch?.Elapsed;
        }

        var dumpsWatch = stats && (dumpLexer || dumpParser) ? Stopwatch.StartNew() : null;
        var tokensDump = dumpLexer
            ? StageDumper.DumpTokens(sql, lexed.Tokens, lexed.Trivia)
            : string.Empty;
        var treeDump = dumpParser && statement is not null
            ? StageDumper.DumpTree(sql, statement)
            : null;
        var dumpTime = dumpsWatch?.Elapsed;

        return new SqlTrace
        {
            Lexer = tokensDump,
            Parser = treeDump,
            Format = formatted,
            Error = null,
            Stats = new SqlStats
            {
                SourceChars = stats ? sql.Length : 0,
                SourceLines = sourceLines,
                TokenCount = tokenCount,
                Comments = comments,
                OutputChars = formatted?.Length,
                Lexer = lexerTime,
                Parser = parserTime,
                Formatter = formatterTime,
                Dump = dumpTime,
                Total = total?.Elapsed ?? TimeSpan.Zero,
                FailedStage = null,
            },
        };
    }

    private static SqlTrace Fail(
        string lexerDump,
        SqlParseException error,
        int sourceChars,
        int sourceLines,
        int tokenCount,
        int comments,
        TimeSpan lexerTime,
        TimeSpan? parserTime,
        TimeSpan? dumpTime,
        TimeSpan total,
        string failedStage)
    {
        return new SqlTrace
        {
            Lexer = lexerDump,
            Parser = null,
            Format = null,
            Error = error,
            Stats = new SqlStats
            {
                SourceChars = sourceChars,
                SourceLines = sourceLines,
                TokenCount = tokenCount,
                Comments = comments,
                OutputChars = null,
                Lexer = lexerTime,
                Parser = parserTime,
                Formatter = null,
                Dump = dumpTime,
                Total = total,
                FailedStage = failedStage,
            },
        };
    }

    private static int CountTokens(IReadOnlyList<SyntaxToken> tokens)
    {
        var count = 0;
        foreach (var token in tokens)
        {
            if (token.Kind != SyntaxKind.EndOfFile)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountComments(IReadOnlyList<SyntaxTrivia> trivia)
    {
        var count = 0;
        foreach (var item in trivia)
        {
            if (item.Kind is SyntaxKind.LineCommentTrivia or SyntaxKind.BlockCommentTrivia)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountLines(string sql)
    {
        if (sql.Length == 0)
        {
            return 0;
        }

        var lines = 1;
        foreach (var ch in sql)
        {
            if (ch == '\n')
            {
                lines++;
            }
        }

        return lines;
    }
}
