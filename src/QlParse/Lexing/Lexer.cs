namespace QlParse;

internal sealed class Lexer
{
    private const int CommentDelimiterLength = 2;
    private const int TwoCharTokenLength = 2;
    private const int ThreeCharTokenLength = 3;
    private const int UnicodeDelimitedPrefixLength = 2;
    private const int CharsPerTokenEstimate = 4;
    private const char StringQuote = '\'';
    private const char IdentifierQuote = '"';
    private const char Ampersand = '&';
    private const char Introducer = '_';
    private const char AtSign = '@';
    private const int IntroducerNameMinLength = 2;

    private readonly string _source;
    private readonly int _length;
    private readonly SqlOptions _flags;
    private readonly List<SyntaxTrivia> _trivia = [];
    private int _position;

    public Lexer(string source, SqlOptions flags = SqlOptions.None)
    {
        _source = source;
        _length = source.Length;
        _flags = flags;
    }

    public IReadOnlyList<SyntaxTrivia> Trivia => _trivia;

    public static SqlLexResult LexAll(string source, SqlOptions flags = SqlOptions.None)
    {
        var lexer = new Lexer(source, flags);
        var tokens = new List<SyntaxToken>(Math.Max(1, source.Length / CharsPerTokenEstimate));
        try
        {
            SyntaxToken token;
            do
            {
                token = lexer.NextToken();
                tokens.Add(token);
            }
            while (token.Kind != SyntaxKind.EndOfFile);

            return new SqlLexResult
            {
                Source = source,
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = null,
            };
        }
        catch (SqlParseException ex)
        {
            return new SqlLexResult
            {
                Source = source,
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = ex,
            };
        }
    }

    public SyntaxToken NextToken()
    {
        var triviaStart = _trivia.Count;
        ReadTrivia();
        var triviaCount = _trivia.Count - triviaStart;
        return TryReadSpecialToken(triviaStart, triviaCount) ?? ReadPunctuationToken(triviaStart, triviaCount);
    }

    private SyntaxToken? TryReadSpecialToken(int triviaStart, int triviaCount)
    {
        if (_position >= _length)
        {
            return new SyntaxToken(SyntaxKind.EndOfFile, _position, 0, triviaStart, triviaCount);
        }

        var ch = _source[_position];
        if (IsEscapeStringStart(ch))
        {
            return ReadEscapeString(triviaStart, triviaCount);
        }

        if (IsPrefixedStringStart(ch))
        {
            return ReadString(triviaStart, triviaCount, prefixed: true);
        }

        if (IsUnicodeDelimitedIdentifierStart(ch))
        {
            return ReadUnicodeDelimitedIdentifier(triviaStart, triviaCount);
        }

        if (IsUnicodeStringStart(ch))
        {
            return ReadUnicodeString(triviaStart, triviaCount);
        }

        if (ch == '$' && (_flags & SqlOptions.Postgres) != 0)
        {
            var dollarQuoted = TryReadDollarQuotedString(triviaStart, triviaCount);
            if (dollarQuoted is not null)
            {
                return dollarQuoted;
            }
        }

        if (IsIdentifierStart(ch))
        {
            return ReadIdentifierOrKeyword(triviaStart, triviaCount);
        }

        if (char.IsAsciiDigit(ch))
        {
            return ReadNumber(triviaStart, triviaCount);
        }

        return null;
    }

    private SyntaxToken ReadPunctuationToken(int triviaStart, int triviaCount)
    {
        var ch = _source[_position];
        return TryReadPunctuationA(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationB(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationC(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationD(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationE(ch, triviaStart, triviaCount)
            ?? throw new SqlParseException($"Unexpected character '{ch}'", _position);
    }

    private SyntaxToken? TryReadPunctuationA(char ch, int triviaStart, int triviaCount) => ch switch
    {
        ',' => ReadSingle(SyntaxKind.Comma, triviaStart, triviaCount),
        '=' => ReadSingle(SyntaxKind.EqualsToken, triviaStart, triviaCount),
        '.' => ReadSingle(SyntaxKind.Dot, triviaStart, triviaCount),
        ';' => ReadSingle(SyntaxKind.Semicolon, triviaStart, triviaCount),
        ':' => ReadColon(triviaStart, triviaCount),
        '|' => Peek() == '|'
            ? ReadTwo(SyntaxKind.ConcatToken, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.BarToken, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationB(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '{' => ReadSingle(SyntaxKind.OpenBrace, triviaStart, triviaCount),
        '}' => ReadSingle(SyntaxKind.CloseBrace, triviaStart, triviaCount),
        '^' => ReadSingle(SyntaxKind.CaretToken, triviaStart, triviaCount),
        '$' => ReadSingle(SyntaxKind.DollarToken, triviaStart, triviaCount),
        '(' => ReadSingle(SyntaxKind.OpenParen, triviaStart, triviaCount),
        ')' => ReadSingle(SyntaxKind.CloseParen, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationC(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '[' => ReadSingle(SyntaxKind.OpenBracket, triviaStart, triviaCount),
        ']' => ReadSingle(SyntaxKind.CloseBracket, triviaStart, triviaCount),
        '>' => Peek() == '='
            ? ReadTwo(SyntaxKind.GreaterOrEqual, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.GreaterThan, triviaStart, triviaCount),
        '<' => ReadLessThan(triviaStart, triviaCount),
        '!' => Peek() == '='
            ? ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount)
            : throw new SqlParseException("Unexpected character '!'", _position),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationD(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '*' => ReadSingle(SyntaxKind.Star, triviaStart, triviaCount),
        '+' => ReadSingle(SyntaxKind.PlusToken, triviaStart, triviaCount),
        '-' => ReadMinus(triviaStart, triviaCount),
        '/' => ReadSingle(SyntaxKind.SlashToken, triviaStart, triviaCount),
        '?' => ReadSingle(SyntaxKind.QuestionMark, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationE(char ch, int triviaStart, int triviaCount) => ch switch
    {
        AtSign => (_flags & SqlOptions.AtParameters) != 0 && IsIdentifierStart(Peek())
            ? ReadEmbeddedHost(triviaStart, triviaCount)
            : throw new SqlParseException($"Unexpected character '{AtSign}'", _position),
        StringQuote => ReadString(triviaStart, triviaCount, prefixed: false),
        IdentifierQuote => ReadQuotedIdentifier(triviaStart, triviaCount),
        _ => null,
    };

    private void ReadTrivia()
    {
        while (_position < _length)
        {
            var ch = _source[_position];
            if (IsWhitespace(ch))
            {
                SkipWhitespace();
                continue;
            }

            if (ch == '-' && Peek() == '-')
            {
                _trivia.Add(ReadLineComment());
                continue;
            }

            if (ch == '/' && Peek() == '*')
            {
                _trivia.Add(ReadBlockComment());
                continue;
            }

            break;
        }
    }

    private void SkipWhitespace()
    {
        while (_position < _length && IsWhitespace(_source[_position]))
        {
            _position++;
        }
    }

    private SyntaxTrivia ReadLineComment()
    {
        var start = _position;
        _position += CommentDelimiterLength;
        while (_position < _length && _source[_position] is not '\n' and not '\r')
        {
            _position++;
        }

        return new SyntaxTrivia(SyntaxKind.LineCommentTrivia, start, _position - start);
    }

    private SyntaxTrivia ReadBlockComment()
    {
        var start = _position;
        _position += CommentDelimiterLength;
        var depth = 1;
        while (_position < _length && depth > 0)
        {
            if (_source[_position] == '/' && Peek() == '*')
            {
                depth++;
                _position += CommentDelimiterLength;
                continue;
            }

            if (_source[_position] == '*' && Peek() == '/')
            {
                depth--;
                _position += CommentDelimiterLength;
                continue;
            }

            _position++;
        }

        if (depth != 0)
        {
            throw new SqlParseException("Unterminated block comment", start);
        }

        return new SyntaxTrivia(SyntaxKind.BlockCommentTrivia, start, _position - start);
    }

    private SyntaxToken ReadIdentifierOrKeyword(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        while (_position < _length && IsIdentifierPart(_source[_position]))
        {
            _position++;
        }

        var text = _source.AsSpan(start, _position - start);
        if (IsCharacterSetIntroducer(text))
        {
            if (_position < _length && _source[_position] == IdentifierQuote)
            {
                return ReadQuotedIdentifier(triviaStart, triviaCount, start);
            }

            if (_position < _length && _source[_position] == StringQuote)
            {
                return ReadString(triviaStart, triviaCount, start);
            }
        }

        return new SyntaxToken(Keyword.Classify(text), start, text.Length, triviaStart, triviaCount);
    }

    private SyntaxToken ReadNumber(int triviaStart, int triviaCount)
    {
        var start = _position;
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }

        if (_position < _length
            && _source[_position] == '.'
            && _position + 1 < _length
            && char.IsAsciiDigit(_source[_position + 1]))
        {
            _position++;
            while (_position < _length && char.IsAsciiDigit(_source[_position]))
            {
                _position++;
            }
        }

        TryReadExponent();
        return new SyntaxToken(SyntaxKind.Number, start, _position - start, triviaStart, triviaCount);
    }

    private void TryReadExponent()
    {
        if (_position >= _length || _source[_position] is not 'E' and not 'e')
        {
            return;
        }

        var look = _position + 1;
        if (look < _length && _source[look] is '+' or '-')
        {
            look++;
        }

        if (look >= _length || !char.IsAsciiDigit(_source[look]))
        {
            return;
        }

        _position = look;
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }
    }

    private SyntaxToken ReadColon(int triviaStart, int triviaCount)
    {
        if (Peek() == ':')
        {
            return ReadTwo(SyntaxKind.DoubleColonToken, triviaStart, triviaCount);
        }

        if (IsIdentifierStart(Peek()))
        {
            return ReadEmbeddedHost(triviaStart, triviaCount);
        }

        return ReadSingle(SyntaxKind.ColonToken, triviaStart, triviaCount);
    }

    private SyntaxToken ReadLessThan(int triviaStart, int triviaCount)
    {
        if (Peek() == '=')
        {
            return ReadTwo(SyntaxKind.LessOrEqual, triviaStart, triviaCount);
        }

        if (Peek() == '>')
        {
            return ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount);
        }

        return ReadSingle(SyntaxKind.LessThan, triviaStart, triviaCount);
    }

    private SyntaxToken ReadMinus(int triviaStart, int triviaCount)
    {
        if (Peek() != '>')
        {
            return ReadSingle(SyntaxKind.MinusToken, triviaStart, triviaCount);
        }

        if (PeekTwo() == '>')
        {
            return ReadThree(SyntaxKind.JsonTextArrowToken, triviaStart, triviaCount);
        }

        return ReadTwo(SyntaxKind.JsonArrowToken, triviaStart, triviaCount);
    }

    private SyntaxToken ReadSingle(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        return new SyntaxToken(kind, start, _position - start, triviaStart, triviaCount);
    }

    private SyntaxToken ReadTwo(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += TwoCharTokenLength;
        return new SyntaxToken(kind, start, TwoCharTokenLength, triviaStart, triviaCount);
    }

    private SyntaxToken ReadThree(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += ThreeCharTokenLength;
        return new SyntaxToken(kind, start, ThreeCharTokenLength, triviaStart, triviaCount);
    }

    private SyntaxToken ReadEmbeddedHost(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        while (_position < _length && IsIdentifierPart(_source[_position]))
        {
            _position++;
        }

        return new SyntaxToken(SyntaxKind.EmbeddedHost, start, _position - start, triviaStart, triviaCount);
    }

    private bool IsPrefixedStringStart(char ch) =>
        (ch is 'N' or 'n' or 'B' or 'b' or 'X' or 'x') && Peek() == StringQuote;

    private bool IsUnicodeDelimitedIdentifierStart(char ch) =>
        (ch is 'U' or 'u') && Peek() == Ampersand && PeekTwo() == IdentifierQuote;

    private bool IsUnicodeStringStart(char ch) =>
        (ch is 'U' or 'u') && Peek() == Ampersand && PeekTwo() == StringQuote;

    private SyntaxToken ReadUnicodeDelimitedIdentifier(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += UnicodeDelimitedPrefixLength;
        var token = ReadQuotedIdentifier(triviaStart, triviaCount, start);
        return ExtendWithUescapeClause(token, start);
    }

    private SyntaxToken ReadUnicodeString(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += UnicodeDelimitedPrefixLength;
        var token = ReadString(triviaStart, triviaCount, start);
        return ExtendWithUescapeClause(token, start);
    }

    private bool IsEscapeStringStart(char ch) =>
        (_flags & SqlOptions.Postgres) != 0 && ch is 'E' or 'e' && Peek() == StringQuote;

    private SyntaxToken ReadEscapeString(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += TwoCharTokenLength;
        while (_position < _length)
        {
            if (_source[_position] == '\\' && _position + 1 < _length)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            if (_source[_position] != StringQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == StringQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated string", start);
    }

    private SyntaxToken? TryReadDollarQuotedString(int triviaStart, int triviaCount)
    {
        var start = _position;
        var tagEnd = FindDollarQuoteTagEnd(start);
        if (tagEnd is null)
        {
            return null;
        }

        var tag = _source[start..(tagEnd.Value + 1)];
        var contentStart = tagEnd.Value + 1;
        var closeIndex = _source.IndexOf(tag, contentStart, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            throw new SqlParseException("Unterminated dollar-quoted string", start);
        }

        _position = closeIndex + tag.Length;
        return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
    }

    private int? FindDollarQuoteTagEnd(int dollarPosition)
    {
        var index = dollarPosition + 1;
        if (index >= _length)
        {
            return null;
        }

        if (_source[index] == '$')
        {
            return index;
        }

        if (!IsIdentifierStart(_source[index]))
        {
            return null;
        }

        index++;
        while (index < _length && _source[index] != '$' && IsIdentifierPart(_source[index]))
        {
            index++;
        }

        return index < _length && _source[index] == '$' ? index : null;
    }

    private SyntaxToken ExtendWithUescapeClause(SyntaxToken token, int start)
    {
        TryConsumeUescapeClause();
        var length = _position - start;
        return length == token.Length ? token : token with { Length = length };
    }

    private void TryConsumeUescapeClause()
    {
        if ((_flags & SqlOptions.Postgres) == 0)
        {
            return;
        }

        var resume = _position;
        SkipWhitespace();
        if (!TryMatchWordAhead("UESCAPE"))
        {
            _position = resume;
            return;
        }

        SkipWhitespace();
        if (!TryReadEscapeCharLiteralLength(out var length))
        {
            _position = resume;
            return;
        }

        _position += length;
    }

    private bool TryMatchWordAhead(string word)
    {
        if (_position + word.Length > _length
            || !_source.AsSpan(_position, word.Length).Equals(word, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var afterPosition = _position + word.Length;
        if (afterPosition < _length && IsIdentifierPart(_source[afterPosition]))
        {
            return false;
        }

        _position = afterPosition;
        return true;
    }

    private bool TryReadEscapeCharLiteralLength(out int length)
    {
        length = 0;
        const int EscapeCharLiteralLength = 3;
        if (_position + EscapeCharLiteralLength > _length
            || _source[_position] != StringQuote
            || _source[_position + 1] == StringQuote
            || _source[_position + 2] != StringQuote)
        {
            return false;
        }

        length = EscapeCharLiteralLength;
        return true;
    }

    private SyntaxToken ReadString(int triviaStart, int triviaCount, bool prefixed)
    {
        var start = _position;
        if (prefixed)
        {
            _position++;
        }

        return ReadString(triviaStart, triviaCount, start);
    }

    private SyntaxToken ReadString(int triviaStart, int triviaCount, int start)
    {
        _position++;
        while (_position < _length)
        {
            if (_source[_position] != StringQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == StringQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated string", start);
    }

    private static bool IsCharacterSetIntroducer(ReadOnlySpan<char> text) =>
        text.Length >= IntroducerNameMinLength && text[0] == Introducer;

    private SyntaxToken ReadQuotedIdentifier(int triviaStart, int triviaCount) =>
        ReadQuotedIdentifier(triviaStart, triviaCount, _position);

    private SyntaxToken ReadQuotedIdentifier(int triviaStart, int triviaCount, int start)
    {
        var quoteStart = _position;
        _position++;
        while (_position < _length)
        {
            if (_source[_position] != IdentifierQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == IdentifierQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            if (_position - quoteStart == TwoCharTokenLength)
            {
                throw new SqlParseException("Empty quoted identifier", start);
            }

            return new SyntaxToken(SyntaxKind.Identifier, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated quoted identifier", start);
    }

    private char Peek() => _position + 1 < _length ? _source[_position + 1] : '\0';
    private char PeekTwo() => _position + TwoCharTokenLength < _length ? _source[_position + TwoCharTokenLength] : '\0';

    private static bool IsWhitespace(char ch) =>
        ch is ' ' or '\t' or '\n' or '\r' || char.IsWhiteSpace(ch);

    private static bool IsIdentifierStart(char ch) =>
        char.IsAsciiLetter(ch) || ch == Introducer || char.IsLetter(ch);

    private static bool IsIdentifierPart(char ch) =>
        char.IsAsciiLetterOrDigit(ch) || ch == Introducer || ch == '$' || char.IsLetterOrDigit(ch);
}
