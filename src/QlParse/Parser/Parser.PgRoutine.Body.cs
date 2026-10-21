namespace QlParse;

// Eager routine body parsing. The body text inside a dollar-quoted string is lexed in place (the lexer
// is bounded to the content range of the same source string, so every token, trivia and error position
// is absolute and nothing is copied) and parsed by a nested parser over those tokens.
// - LANGUAGE plpgsql: dollar-quoted bodies only. A single-quoted body keeps just the raw string, because
//   its doubled '' quotes would need unescaping before it could be lexed.
// - LANGUAGE sql: dollar-quoted bodies, and single-quoted bodies whose content holds no quote at all.
// - Any other language (c, internal, plperl, ...) keeps the raw string.
// Comments inside the body are collected by the nested lexer into BodyTrivia; the tokens in BodyTokens and
// in the body nodes index into that list, never into the outer result's Trivia, which is left untouched.
internal sealed partial class Parser
{
    private const char DollarQuote = '$';
    private const char SingleQuote = '\'';

    internal enum PgBodyLanguage
    {
        Other,
        Sql,
        Plpgsql,
    }

    private readonly record struct PgRoutineBody(Query? Body, IReadOnlyList<SyntaxToken> Tokens, IReadOnlyList<SyntaxTrivia> Trivia);

    private PgBodyLanguage PgBodyLanguageOf(IReadOnlyList<RoutineCharacteristic> characteristics)
    {
        foreach (var characteristic in characteristics)
        {
            if (TokenEquals(characteristic.Name, Keyword.Language))
            {
                return PgBodyLanguageOf(characteristic.Tail[0]);
            }
        }

        return PgBodyLanguage.Other;
    }

    private PgBodyLanguage PgBodyLanguageOf(SyntaxToken language)
    {
        var text = language.TextOf(_source);
        if (language.Kind == SyntaxKind.String || text[0] == '"')
        {
            text = text[1..^1];
        }

        if (TextEquals(text, Keyword.RoutinePlpgsql))
        {
            return PgBodyLanguage.Plpgsql;
        }

        return TextEquals(text, Keyword.Sql) ? PgBodyLanguage.Sql : PgBodyLanguage.Other;
    }

    private PgRoutineBody ParsePgRoutineBody(SyntaxToken code, PgBodyLanguage language)
    {
        var range = PgBodyContentRange(code, language);
        if (range.Length < 0)
        {
            return new PgRoutineBody(null, [], []);
        }

        var (tokens, trivia) = Lexer.LexRange(_source, range.Position, range.End, _flags);
        if (language == PgBodyLanguage.Plpgsql)
        {
            MergePlpgsqlRangeDots(tokens);
            return new PgRoutineBody(new Parser(tokens, _source, _flags).ParsePlpgsqlBody(), tokens, trivia);
        }

        var body = tokens.Count == 1 ? null : Parse(tokens, _source, _flags);
        return new PgRoutineBody(body, tokens, trivia);
    }

    // The content between the quotes, or a negative length when the body is not parsed eagerly.
    private SourceSpan PgBodyContentRange(SyntaxToken code, PgBodyLanguage language)
    {
        var none = new SourceSpan(code.Position, -1);
        if (language == PgBodyLanguage.Other)
        {
            return none;
        }

        var first = _source[code.Position];
        if (first == DollarQuote)
        {
            var tagEnd = code.Position + 1;
            while (_source[tagEnd] != DollarQuote)
            {
                tagEnd++;
            }

            var tagLength = tagEnd - code.Position + 1;
            return new SourceSpan(code.Position + tagLength, code.Length - (tagLength * Len2));
        }

        if (first != SingleQuote || language != PgBodyLanguage.Sql)
        {
            return none;
        }

        var content = new SourceSpan(code.Position + 1, code.Length - Len2);
        return _source.AsSpan(content.Position, content.Length).Contains(SingleQuote) ? none : content;
    }

    // `..` (the FOR loop range) is two Dot tokens to the lexer; inside a PL/pgSQL body two adjacent dots
    // become one DotDotToken, so the expression parser stops at it.
    private static void MergePlpgsqlRangeDots(List<SyntaxToken> tokens)
    {
        var write = 0;
        var read = 0;
        while (read < tokens.Count)
        {
            var token = tokens[read];
            read++;
            if (token.Kind == SyntaxKind.Dot && read < tokens.Count && tokens[read].Kind == SyntaxKind.Dot
                && tokens[read].Position == token.Position + 1)
            {
                token = token with { Kind = SyntaxKind.DotDotToken, Length = Len2 };
                read++;
            }

            tokens[write] = token;
            write++;
        }

        tokens.RemoveRange(write, tokens.Count - write);
    }
}
